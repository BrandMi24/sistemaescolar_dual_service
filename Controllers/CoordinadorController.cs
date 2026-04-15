using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ControlEscolar.Controllers
{
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Management _repo;
        private readonly ILogger<CoordinadorController> _logger;

        public CoordinadorController(ApplicationDbContext context, ILogger<CoordinadorController> logger)
        {
            _context = context;
            _repo = new Management(context);
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Ciclos()
        {
            return View();
        }

        public IActionResult Catalogos(string? tab = null)
        {
            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "tab-personas" : tab;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToUsuario
                );

                var data = lista
                    .Where(u =>
                        u.PersonId > 0 &&
                        (
                            u.Id > 0 ||
                            (u.IdentificadorTipo == "ALUMNO" && u.RelatedEntityId.HasValue && u.RelatedEntityId.Value > 0) ||
                            (u.IdentificadorTipo == "DOCENTE" && u.RelatedEntityId.HasValue && u.RelatedEntityId.Value > 0)
                        )
                    )
                    .Select(u => new
                    {
                        id = u.Id,
                        username = string.IsNullOrWhiteSpace(u.Username) ? "-" : u.Username,
                        identificador = string.IsNullOrWhiteSpace(u.Identificador) ? "-" : u.Identificador,
                        personId = u.PersonId,
                        studentId = u.IdentificadorTipo == "ALUMNO" ? u.RelatedEntityId : (int?)null,
                        teacherId = u.IdentificadorTipo == "DOCENTE" ? u.RelatedEntityId : (int?)null,
                        nombreCompleto = $"{u.LastNamePaternal} {u.LastNameMaternal}, {u.FirstName}".Replace("  ", " ").Trim().Trim(','),
                        roles = string.IsNullOrWhiteSpace(u.Roles) ? "Sin Rol" : u.Roles,
                        tipoUsuario = string.IsNullOrWhiteSpace(u.TipoUsuario) ? "ADMIN" : u.TipoUsuario,
                        carrera = string.IsNullOrWhiteSpace(u.Carrera) ? "-" : u.Carrera,
                        correo = string.IsNullOrWhiteSpace(u.Correo) ? "-" : u.Correo,
                        estado = string.IsNullOrWhiteSpace(u.Estado) ? "INACTIVO" : u.Estado
                    })
                    .GroupBy(x => new
                    {
                        x.personId,
                        x.tipoUsuario,
                        x.identificador,
                        x.username
                    })
                    .Select(g => g.First())
                    .ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<Dictionary<int, string>> GetAvailableStudentsForAccountAsync(int? currentPersonId = null)
        {
            var students = await _repo.ExecuteStoredProcedureAsync(
                "getview_student_full",
                new Dictionary<string, object>
                {
            { "@Status", DBNull.Value },
            { "@Student_IsFolio", DBNull.Value }
                },
                ModelMappers.MapToStudent
            );

            var users = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                null,
                ModelMappers.MapToUsuario
            );

            var occupiedPersonIds = users
                .Where(u => u.Id > 0 && u.PersonId > 0)
                .Select(u => u.PersonId)
                .Distinct()
                .ToHashSet();

            return students
                .Where(s =>
                    s.PersonId > 0 &&
                    (!occupiedPersonIds.Contains(s.PersonId) || s.PersonId == currentPersonId))
                .GroupBy(s => s.Id)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var x = g.First();
                        var identificador = !string.IsNullOrWhiteSpace(x.Matricula) ? x.Matricula : x.Folio ?? "S/N";
                        var nombre = $"{x.ApellidoPaterno} {x.ApellidoMaterno}, {x.Nombres}".Trim();
                        return $"{identificador} - {nombre}";
                    }
                );
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser(string? tab = null)
        {
            ViewBag.Roles = await _repo.GetRolesAsync();
            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync();
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-cuentas" : tab;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-cuentas" : returnTab;

            if (ModelState.IsValid)
            {
                try
                {
                    var roles = await _repo.GetRolesAsync();

                    if (!int.TryParse(model.Role, out int roleId))
                    {
                        ModelState.AddModelError("Role", "Rol inválido.");
                        ViewBag.Roles = roles;
                        ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync();
                        ViewBag.IsEdit = false;
                        ViewBag.ReturnTab = returnTab;
                        return View(model);
                    }

                    var roleName = roles.ContainsKey(roleId) ? roles[roleId].ToUpperInvariant() : string.Empty;
                    var isStudentRole =
                        roleName.Contains("STUDENT") ||
                        roleName.Contains("ALUMNO") ||
                        roleName.Contains("ESTUDIANTE");

                    int personId;

                    if (isStudentRole && model.ExistingStudentId.HasValue && model.ExistingStudentId.Value > 0)
                    {
                        var studentData = await _repo.ExecuteStoredProcedureAsync(
                            "getview_student_full",
                            new Dictionary<string, object>
                            {
                        { "@ID", model.ExistingStudentId.Value },
                        { "@Status", DBNull.Value },
                        { "@Student_IsFolio", DBNull.Value }
                            },
                            ModelMappers.MapToStudent
                        );

                        var student = studentData.FirstOrDefault();
                        if (student == null)
                        {
                            ModelState.AddModelError("", "No se encontró el alumno seleccionado.");
                            ViewBag.Roles = roles;
                            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync();
                            ViewBag.IsEdit = false;
                            ViewBag.ReturnTab = returnTab;
                            return View(model);
                        }

                        personId = student.PersonId;

                        var linkedUsers = await _repo.ExecuteStoredProcedureAsync(
                            "getview_user_full",
                            null,
                            ModelMappers.MapToUsuario
                        );

                        if (linkedUsers.Any(u => u.PersonId == personId && u.Id > 0))
                        {
                            ModelState.AddModelError("", "Ese alumno ya tiene una cuenta vinculada.");
                            ViewBag.Roles = roles;
                            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync();
                            ViewBag.IsEdit = false;
                            ViewBag.ReturnTab = returnTab;
                            return View(model);
                        }

                        await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
                {
                    { "@ID", personId },
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", model.Email }
                });
                    }
                    else
                    {
                        personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
                {
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", model.Email },
                    { "@Phone", DBNull.Value },
                    { "@CURP", DBNull.Value }
                });
                    }

                    var userId = await _repo.CreateUserAsync(new Dictionary<string, object>
            {
                { "@UserPersonID", personId },
                { "@Username", model.Username },
                { "@UserEmail", model.Email },
                { "@PasswordHash", HashPassword(model.Password ?? string.Empty) }
            });

                    await _repo.CreateUserRoleAsync(new Dictionary<string, object>
            {
                { "@UserRole_UserID", userId },
                { "@UserRole_RoleID", roleId }
            });

                    TempData["SuccessMessage"] = "Cuenta creada correctamente.";
                    return RedirectToAction("Catalogos", new { tab = returnTab });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Error al procesar la solicitud: " + ex.Message);
                }
            }

            ViewBag.Roles = await _repo.GetRolesAsync();
            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync();
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = returnTab;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id, string? tab = null)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToUsuario
            );

            var user = result.FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _repo.GetRolesAsync();
            var roleId = roles.FirstOrDefault(r =>
                (user.Roles ?? string.Empty).Contains(r.Value, StringComparison.OrdinalIgnoreCase)).Key;

            int? linkedStudentId = null;

            var students = await _repo.ExecuteStoredProcedureAsync(
                "getview_student_full",
                new Dictionary<string, object>
                {
            { "@Status", DBNull.Value },
            { "@Student_IsFolio", DBNull.Value }
                },
                ModelMappers.MapToStudent
            );

            var linkedStudent = students.FirstOrDefault(s => s.PersonId == user.PersonId);
            if (linkedStudent != null)
            {
                linkedStudentId = linkedStudent.Id;
            }

            var model = new CreateUserViewModel
            {
                UserId = user.Id,
                PersonId = user.PersonId,
                FirstName = user.FirstName ?? string.Empty,
                LastNamePaternal = user.LastNamePaternal ?? string.Empty,
                LastNameMaternal = user.LastNameMaternal,
                Email = user.Correo,
                Username = user.Username ?? string.Empty,
                Role = roleId > 0 ? roleId.ToString() : string.Empty,
                Password = string.Empty,
                ExistingStudentId = linkedStudentId
            };

            ViewBag.Roles = roles;
            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync(user.PersonId);
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-cuentas" : tab;

            return View("CreateUser", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(CreateUserViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-cuentas" : returnTab;

            if (model.UserId == null || model.PersonId == null)
            {
                ModelState.AddModelError("", "IDs no recibidos correctamente.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _repo.GetRolesAsync();
                ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync(model.PersonId);
                ViewBag.IsEdit = true;
                ViewBag.ReturnTab = returnTab;
                return View("CreateUser", model);
            }

            try
            {
                var roles = await _repo.GetRolesAsync();

                int targetPersonId = model.PersonId.Value;

                if (int.TryParse(model.Role, out int roleId))
                {
                    var roleName = roles.ContainsKey(roleId) ? roles[roleId].ToUpperInvariant() : string.Empty;
                    var isStudentRole =
                        roleName.Contains("STUDENT") ||
                        roleName.Contains("ALUMNO") ||
                        roleName.Contains("ESTUDIANTE");

                    if (isStudentRole && model.ExistingStudentId.HasValue && model.ExistingStudentId.Value > 0)
                    {
                        var studentData = await _repo.ExecuteStoredProcedureAsync(
                            "getview_student_full",
                            new Dictionary<string, object>
                            {
                        { "@ID", model.ExistingStudentId.Value },
                        { "@Status", DBNull.Value },
                        { "@Student_IsFolio", DBNull.Value }
                            },
                            ModelMappers.MapToStudent
                        );

                        var student = studentData.FirstOrDefault();
                        if (student == null)
                        {
                            ModelState.AddModelError("", "No se encontró el alumno seleccionado.");
                            ViewBag.Roles = roles;
                            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync(model.PersonId);
                            ViewBag.IsEdit = true;
                            ViewBag.ReturnTab = returnTab;
                            return View("CreateUser", model);
                        }

                        var linkedUsers = await _repo.ExecuteStoredProcedureAsync(
                            "getview_user_full",
                            null,
                            ModelMappers.MapToUsuario
                        );

                        bool alumnoOcupadoPorOtroUsuario = linkedUsers.Any(u =>
                            u.PersonId == student.PersonId &&
                            u.Id > 0 &&
                            u.Id != model.UserId.Value);

                        if (alumnoOcupadoPorOtroUsuario)
                        {
                            ModelState.AddModelError("", "Ese alumno ya está vinculado a otra cuenta.");
                            ViewBag.Roles = roles;
                            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync(model.PersonId);
                            ViewBag.IsEdit = true;
                            ViewBag.ReturnTab = returnTab;
                            return View("CreateUser", model);
                        }

                        targetPersonId = student.PersonId;
                    }

                    await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
            {
                { "@ID", targetPersonId },
                { "@FirstName", model.FirstName },
                { "@LastNamePaternal", model.LastNamePaternal },
                { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                { "@Email", model.Email }
            });

                    await _repo.ExecuteCommandAsync(
                        @"UPDATE dbo.management_user_table
                  SET management_user_PersonID = @PersonId,
                      management_user_Username = @Username,
                      management_user_Email = @UserEmail,
                      management_user_PasswordHash = CASE
                          WHEN @PasswordHash IS NULL THEN management_user_PasswordHash
                          ELSE @PasswordHash
                      END
                  WHERE management_user_ID = @ID;",
                        new Dictionary<string, object>
                        {
                    { "@ID", model.UserId.Value },
                    { "@PersonId", targetPersonId },
                    { "@Username", model.Username },
                    { "@UserEmail", model.Email },
                    { "@PasswordHash", string.IsNullOrWhiteSpace(model.Password) ? DBNull.Value : HashPassword(model.Password) }
                        });

                    var rolesActuales = await _repo.ExecuteStoredProcedureAsync(
                        "management_userrole_get",
                        null,
                        r => new
                        {
                            Id = Management.GetValue<int>(r, "management_userrole_ID"),
                            UserId = Management.GetValue<int>(r, "management_userrole_UserID"),
                            RoleId = Management.GetValue<int>(r, "management_userrole_RoleID")
                        }
                    );

                    var rolesUsuario = rolesActuales.Where(x => x.UserId == model.UserId.Value).ToList();

                    foreach (var rol in rolesUsuario.Where(x => x.RoleId != roleId))
                    {
                        await _repo.ExecuteNonQueryAsync("management_userrole_softdelete",
                            new Dictionary<string, object> { { "@ID", rol.Id } });
                    }

                    var yaTieneEseRol = rolesUsuario.Any(x => x.RoleId == roleId);

                    if (!yaTieneEseRol)
                    {
                        await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                {
                    { "@UserRole_UserID", model.UserId.Value },
                    { "@UserRole_RoleID", roleId }
                });
                    }
                }

                TempData["SuccessMessage"] = "Cuenta actualizada correctamente.";
                return RedirectToAction("Catalogos", new { tab = returnTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewBag.Roles = await _repo.GetRolesAsync();
            ViewBag.AvailableStudents = await GetAvailableStudentsForAccountAsync(model.PersonId);
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = returnTab;
            return View("CreateUser", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var users = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    new Dictionary<string, object> { { "@ID", id } },
                    ModelMappers.MapToUsuario
                );

                var user = users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return NotFound();

                var estado = (user.Estado ?? "").ToUpperInvariant();

                // Primer nivel: baja lógica
                if (estado == "ACTIVO" || estado == "INSCRITO" || estado == "PREINSCRITO")
                {
                    await _repo.ExecuteNonQueryAsync(
                        "management_user_softdelete",
                        new Dictionary<string, object> { { "@ID", id } }
                    );

                    return Ok(new { mode = "softdelete" });
                }

                // Segundo nivel: borrar cuenta definitivamente
                await _repo.ExecuteCommandAsync(
                    @"DELETE FROM dbo.management_userrole_table
              WHERE management_userrole_UserID = @ID;

              DELETE FROM dbo.management_usercareer_table
              WHERE management_usercareer_UserID = @ID;

              DELETE FROM dbo.management_user_table
              WHERE management_user_ID = @ID
                AND management_user_status = 0;",
                    new Dictionary<string, object>
                    {
                { "@ID", id }
                    });

                // Si la persona ya no está ligada a nada, bórrala también
                if (user.PersonId > 0)
                {
                    await _repo.ExecuteCommandAsync(
                        @"IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.management_user_table
                        WHERE management_user_PersonID = @PersonId
                   )
                   AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.management_student_table
                        WHERE management_student_PersonID = @PersonId
                   )
                   AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.management_teacher_table
                        WHERE management_teacher_PersonID = @PersonId
                   )
                   BEGIN
                        DELETE FROM dbo.management_person_table
                        WHERE management_person_ID = @PersonId;
                   END",
                        new Dictionary<string, object>
                        {
                    { "@PersonId", user.PersonId }
                        });
                }

                return Ok(new { mode = "harddelete" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleUserStatusRequest request)
        {
            try
            {
                var users = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    new Dictionary<string, object> { { "@ID", request.Id } },
                    ModelMappers.MapToUsuario
                );

                var user = users.FirstOrDefault(u => u.Id == request.Id);
                if (user == null)
                    return NotFound();

                var status = request.Activar ? 1 : 0;

                await _repo.ExecuteCommandAsync(
                    @"UPDATE dbo.management_user_table
              SET management_user_status = @Status
              WHERE management_user_ID = @ID;",
                    new Dictionary<string, object>
                    {
                { "@Status", status },
                { "@ID", request.Id }
                    });

                await _repo.ExecuteCommandAsync(
                    @"UPDATE dbo.management_userrole_table
              SET management_userrole_status = @Status
              WHERE management_userrole_UserID = @ID;",
                    new Dictionary<string, object>
                    {
                { "@Status", status },
                { "@ID", request.Id }
                    });

                await _repo.ExecuteCommandAsync(
                    @"UPDATE dbo.management_usercareer_table
              SET management_usercareer_status = @Status
              WHERE management_usercareer_UserID = @ID;",
                    new Dictionary<string, object>
                    {
                { "@Status", status },
                { "@ID", request.Id }
                    });

                if (user.PersonId > 0)
                {
                    await _repo.ExecuteCommandAsync(
                        @"UPDATE dbo.management_teacher_table
                  SET management_teacher_status = @Status,
                      management_teacher_StatusCode = CASE WHEN @Status = 1 THEN 'ACTIVO' ELSE 'INACTIVO' END
                  WHERE management_teacher_PersonID = @PersonId;",
                        new Dictionary<string, object>
                        {
                    { "@Status", status },
                    { "@PersonId", user.PersonId }
                        });

                    await _repo.ExecuteCommandAsync(
                        @"UPDATE dbo.management_student_table
                  SET management_student_status = @Status,
                      management_student_StatusCode = CASE
                        WHEN @Status = 1 AND management_student_Matricula IS NULL THEN 'PREINSCRITO'
                        WHEN @Status = 1 AND management_student_Matricula IS NOT NULL THEN 'INSCRITO'
                        ELSE 'BAJA'
                      END
                  WHERE management_student_PersonID = @PersonId;",
                        new Dictionary<string, object>
                        {
                    { "@Status", status },
                    { "@PersonId", user.PersonId }
                        });
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAlumnosJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_student_full",
                    new Dictionary<string, object>
                    {
                { "@Status", 1 },
                { "@Student_IsFolio", DBNull.Value }
                    },
                    ModelMappers.MapToStudent
                );

                var data = lista.Select(s => new
                {
                    id = s.Id,
                    identificador = !string.IsNullOrWhiteSpace(s.Matricula) ? s.Matricula : s.Folio ?? "S/N",
                    nombreCompleto = $"{s.ApellidoPaterno} {s.ApellidoMaterno}, {s.Nombres}".Trim(),
                    carrera = s.Carrera,
                    grupo = s.Grupo,
                    semestre = s.Semestre,
                    estadoCodigo = s.EstadoCodigo,
                    esActivo = s.EsActivo,
                    estadoBadgeClass = s.EstadoCodigo == "INSCRITO"
                        ? "status-aceptado"
                        : s.EstadoCodigo == "PREINSCRITO"
                            ? "bg-app border border-theme text-dark"
                            : "status-rechazado"
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAlumnosJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent(string? tab = null)
        {
            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-alumnos" : tab;

            var availableAccountsList = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                null,
                ModelMappers.MapToUsuario
            );

            ViewBag.AvailableAccounts = availableAccountsList
                .Where(x => x.Id > 0 && x.PersonId > 0 && x.TipoUsuario == "ADMIN")
                .GroupBy(x => x.Id)
                .ToDictionary(
                    g => g.Key,
                    g => $"{g.First().Username} - {g.First().Correo}"
                );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-alumnos" : returnTab;

            if (string.IsNullOrWhiteSpace(model.StatusCode))
                model.StatusCode = "INSCRITO";

            if (ModelState.IsValid)
            {
                try
                {
                    var personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
            {
                { "@FirstName", model.FirstName },
                { "@LastNamePaternal", model.LastNamePaternal },
                { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                { "@Email", model.Email },
                { "@Phone", (object?)model.Phone ?? DBNull.Value },
                { "@CURP", string.IsNullOrWhiteSpace(model.CURP) ? DBNull.Value : model.CURP }
            });

                    var isFolio = model.StatusCode == "PREINSCRITO" ? 1 : 0;

                    var studentId = await _repo.CreateStudentAsync(new Dictionary<string, object>
            {
                { "@StudentPersonID", personId },
                { "@StudentCareerID", model.CareerId },
                { "@StudentGroupID", model.GroupId ?? (object)DBNull.Value },
                { "@Student_StatusCode", model.StatusCode },
                { "@Student_IsFolio", isFolio },
                { "@Student_Matricula", DBNull.Value }
            });

                    await _repo.ExecuteNonQueryAsync("management_studentcareer_history_insert",
                        new Dictionary<string, object>
                        {
                    { "@StudentCareerHistory_StudentID", studentId },
                    { "@StudentCareerHistory_CareerID", model.CareerId },
                    { "@StudentCareerHistory_StartDate", DateTime.Now },
                    { "@StudentCareerHistory_EndDate", DBNull.Value },
                    { "@StudentCareerHistory_Reason", "Ingreso inicial" }
                        });

                    if (model.GroupId.HasValue && model.GroupId > 0)
                    {
                        await _repo.ExecuteNonQueryAsync("management_studentgroup_history_insert",
                            new Dictionary<string, object>
                            {
                        { "@StudentGroupHistory_StudentID", studentId },
                        { "@StudentGroupHistory_GroupID", model.GroupId },
                        { "@StudentGroupHistory_StartDate", DateTime.Now },
                        { "@StudentGroupHistory_EndDate", DBNull.Value },
                        { "@StudentGroupHistory_Reason", "Asignación inicial" }
                            });
                    }

                    if (model.StatusCode == "INSCRITO")
                    {
                        var roles = await _repo.GetRolesAsync();
                        var studentRoleId = roles.FirstOrDefault(r =>
                            r.Value.Contains("STUDENT", StringComparison.OrdinalIgnoreCase) ||
                            r.Value.Contains("ALUMNO", StringComparison.OrdinalIgnoreCase) ||
                            r.Value.Contains("ESTUDIANTE", StringComparison.OrdinalIgnoreCase)).Key;

                        if (model.ExistingUserId.HasValue && model.ExistingUserId.Value > 0)
                        {
                            await _repo.ExecuteCommandAsync(
                                @"UPDATE dbo.management_user_table
                          SET management_user_PersonID = @PersonId,
                              management_user_Email = @Email,
                              management_user_status = 1
                          WHERE management_user_ID = @UserId;",
                                new Dictionary<string, object>
                                {
                            { "@PersonId", personId },
                            { "@Email", model.Email },
                            { "@UserId", model.ExistingUserId.Value }
                                });

                            if (studentRoleId > 0)
                            {
                                await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                        {
                            { "@UserRole_UserID", model.ExistingUserId.Value },
                            { "@UserRole_RoleID", studentRoleId }
                        });
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(model.Username) && !string.IsNullOrWhiteSpace(model.Password))
                        {
                            var userId = await _repo.CreateUserAsync(new Dictionary<string, object>
                    {
                        { "@UserPersonID", personId },
                        { "@Username", model.Username },
                        { "@UserEmail", model.Email },
                        { "@PasswordHash", HashPassword(model.Password) }
                    });

                            if (studentRoleId > 0)
                            {
                                await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                        {
                            { "@UserRole_UserID", userId },
                            { "@UserRole_RoleID", studentRoleId }
                        });
                            }
                        }
                    }

                    TempData["SuccessMessage"] = "Estudiante registrado correctamente.";
                    return RedirectToAction("Catalogos", new { tab = returnTab });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = returnTab;

            var availableAccountsList = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                null,
                ModelMappers.MapToUsuario
            );

            ViewBag.AvailableAccounts = availableAccountsList
                .Where(x => x.Id > 0 && x.PersonId > 0 && x.TipoUsuario == "ADMIN")
                .GroupBy(x => x.Id)
                .ToDictionary(
                    g => g.Key,
                    g => $"{g.First().Username} - {g.First().Correo}"
                );

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id, string? tab = null)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "getview_student_full",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToStudent
            );

            var student = result.FirstOrDefault();

            if (student == null)
                return NotFound();

            var model = new CreateStudentViewModel
            {
                StudentId = student.Id,
                PersonId = student.PersonId,
                FirstName = student.Nombres,
                LastNamePaternal = student.ApellidoPaterno,
                LastNameMaternal = student.ApellidoMaterno,
                Email = student.Email ?? string.Empty,
                CURP = student.CURP,
                Phone = student.Phone,
                CareerId = student.CareerId,
                GroupId = student.GroupId,
                StatusCode = string.IsNullOrWhiteSpace(student.EstadoCodigo) ? "INSCRITO" : student.EstadoCodigo,
                Matricula = student.Matricula
            };

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-alumnos" : tab;

            return View("CreateStudent", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(CreateStudentViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-alumnos" : returnTab;

            if (string.IsNullOrWhiteSpace(model.StatusCode))
                model.StatusCode = "INSCRITO";

            if (!ModelState.IsValid)
            {
                ViewBag.Careers = await _repo.GetCareersAsync();
                ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
                ViewBag.IsEdit = true;
                ViewBag.ReturnTab = returnTab;
                return View("CreateStudent", model);
            }

            try
            {
                var oldStudentResult = await _repo.ExecuteStoredProcedureAsync(
                    "getview_student_full",
                    new Dictionary<string, object> { { "@ID", model.StudentId } },
                    ModelMappers.MapToStudent
                );

                var old = oldStudentResult.FirstOrDefault();

                bool cambioCarrera = old != null && old.CareerId != model.CareerId;
                bool cambioGrupo = old != null && old.GroupId != model.GroupId;
                bool pasaAInscrito = old != null && old.EstadoCodigo == "PREINSCRITO" && model.StatusCode == "INSCRITO" && string.IsNullOrWhiteSpace(old.Matricula);

                if (cambioCarrera)
                {
                    await _repo.ExecuteNonQueryAsync("management_studentcareer_history_insert",
                        new Dictionary<string, object>
                        {
                            { "@StudentCareerHistory_StudentID", model.StudentId },
                            { "@StudentCareerHistory_CareerID", model.CareerId },
                            { "@StudentCareerHistory_StartDate", DateTime.Now },
                            { "@StudentCareerHistory_EndDate", DBNull.Value },
                            { "@StudentCareerHistory_Reason", "Cambio de carrera" }
                        });
                }

                if (cambioGrupo)
                {
                    await _repo.ExecuteNonQueryAsync("management_studentgroup_history_insert",
                        new Dictionary<string, object>
                        {
                            { "@StudentGroupHistory_StudentID", model.StudentId },
                            { "@StudentGroupHistory_GroupID", model.GroupId ?? (object)DBNull.Value },
                            { "@StudentGroupHistory_StartDate", DateTime.Now },
                            { "@StudentGroupHistory_EndDate", DBNull.Value },
                            { "@StudentGroupHistory_Reason", "Cambio de grupo" }
                        });
                }

                await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
                {
                    { "@ID", model.PersonId },
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", model.Email },
                    { "@Phone", (object?)model.Phone ?? DBNull.Value },
                    { "@CURP", string.IsNullOrWhiteSpace(model.CURP) ? DBNull.Value : model.CURP }
                });

                await _repo.ExecuteNonQueryAsync("management_student_update", new Dictionary<string, object>
                {
                    { "@ID", model.StudentId },
                    { "@StudentCareerID", model.CareerId },
                    { "@StudentGroupID", model.GroupId ?? (object)DBNull.Value },
                    { "@Student_StatusCode", model.StatusCode }
                });

                if (pasaAInscrito && model.StudentId.HasValue)
                {
                    await _repo.ExecuteStoredProcedureAsync(
                        "management_student_assign_matricula",
                        new Dictionary<string, object>
                        {
                            { "@ID", model.StudentId.Value },
                            { "@Student_StatusCode", "INSCRITO" }
                        },
                        r => 0
                    );
                }

                TempData["SuccessMessage"] = "Alumno actualizado correctamente.";
                return RedirectToAction("Catalogos", new { tab = returnTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = returnTab;
            return View("CreateStudent", model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var info = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToUsuario
                );

                var data = info.FirstOrDefault(x => x.IdentificadorTipo == "ALUMNO" && x.RelatedEntityId == id);

                var studentData = await _repo.ExecuteStoredProcedureAsync(
                    "getview_student_full",
                    new Dictionary<string, object> { { "@ID", id }, { "@Status", DBNull.Value }, { "@Student_IsFolio", DBNull.Value } },
                    ModelMappers.MapToStudent
                );

                var student = studentData.FirstOrDefault();
                if (student == null)
                    return NotFound();

                if (student.EsActivo)
                {
                    await _repo.ExecuteNonQueryAsync("management_student_softdelete",
                        new Dictionary<string, object> { { "@ID", id } });

                    if (data != null && data.Id > 0)
                    {
                        await _repo.ExecuteNonQueryAsync("management_user_softdelete",
                            new Dictionary<string, object> { { "@ID", data.Id } });
                    }

                    return Ok(new { mode = "softdelete" });
                }

                if (data != null && data.Id > 0)
                {
                    await _repo.ExecuteCommandAsync(
                        @"DELETE FROM dbo.management_userrole_table WHERE management_userrole_UserID = @UserId;
                  DELETE FROM dbo.management_usercareer_table WHERE management_usercareer_UserID = @UserId;
                  DELETE FROM dbo.management_user_table WHERE management_user_ID = @UserId AND management_user_status = 0;",
                        new Dictionary<string, object> { { "@UserId", data.Id } });
                }

                await _repo.ExecuteCommandAsync(
                    @"DELETE FROM dbo.management_studentgroup_history_table WHERE management_studentgroup_history_StudentID = @ID;
              DELETE FROM dbo.management_studentcareer_history_table WHERE management_studentcareer_history_StudentID = @ID;
              DELETE FROM dbo.management_student_table WHERE management_student_ID = @ID AND management_student_status = 0;",
                    new Dictionary<string, object> { { "@ID", id } });

                return Ok(new { mode = "harddelete" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult StudentDetail(int id)
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBajasJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToUsuario
                );

                var data = lista
                    .Where(u =>
                        string.Equals(u.Estado, "BAJA", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.Estado, "INACTIVO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.Estado, "LICENCIA", StringComparison.OrdinalIgnoreCase))
                    .Select(u => new
                    {
                        id = u.RelatedEntityId > 0 ? u.RelatedEntityId : u.Id,
                        userId = u.Id,
                        identificador = string.IsNullOrWhiteSpace(u.Identificador) ? "-" : u.Identificador,
                        nombreCompleto = $"{u.LastNamePaternal} {u.LastNameMaternal}, {u.FirstName}".Trim(),
                        tipoUsuario = string.IsNullOrWhiteSpace(u.TipoUsuario) ? "ADMIN" : u.TipoUsuario.ToUpperInvariant(),
                        email = string.IsNullOrWhiteSpace(u.Correo) ? "-" : u.Correo,
                        estadoCodigo = string.IsNullOrWhiteSpace(u.Estado) ? "INACTIVO" : u.Estado.ToUpperInvariant(),
                        estadoBadgeClass = "status-rechazado"
                    })
                    .ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBajasJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInactiveRecord([FromBody] DeleteInactiveRecordRequest request)
        {
            try
            {
                var tipo = (request.Tipo ?? string.Empty).ToUpperInvariant();

                if (tipo == "ALUMNO")
                {
                    if (request.UserId > 0)
                    {
                        await _repo.ExecuteCommandAsync(
                            @"DELETE FROM dbo.management_userrole_table WHERE management_userrole_UserID = @UserId;
                      DELETE FROM dbo.management_usercareer_table WHERE management_usercareer_UserID = @UserId;
                      DELETE FROM dbo.management_user_table WHERE management_user_ID = @UserId AND management_user_status = 0;",
                            new Dictionary<string, object> { { "@UserId", request.UserId } });
                    }

                    await _repo.ExecuteCommandAsync(
                        @"DELETE FROM dbo.management_studentgroup_history_table WHERE management_studentgroup_history_StudentID = @ID;
                  DELETE FROM dbo.management_studentcareer_history_table WHERE management_studentcareer_history_StudentID = @ID;
                  DELETE FROM dbo.management_student_table WHERE management_student_ID = @ID AND management_student_status = 0;",
                        new Dictionary<string, object> { { "@ID", request.Id } });

                    return Ok();
                }

                if (tipo == "DOCENTE")
                {
                    if (request.UserId > 0)
                    {
                        await _repo.ExecuteCommandAsync(
                            @"DELETE FROM dbo.management_userrole_table WHERE management_userrole_UserID = @UserId;
                      DELETE FROM dbo.management_usercareer_table WHERE management_usercareer_UserID = @UserId;
                      DELETE FROM dbo.management_user_table WHERE management_user_ID = @UserId AND management_user_status = 0;",
                            new Dictionary<string, object> { { "@UserId", request.UserId } });
                    }

                    await _repo.ExecuteCommandAsync(
                        @"DELETE FROM dbo.management_teacher_table
                  WHERE management_teacher_ID = @ID
                    AND management_teacher_status = 0;",
                        new Dictionary<string, object> { { "@ID", request.Id } });

                    return Ok();
                }

                if (tipo == "ADMIN")
                {
                    await _repo.ExecuteCommandAsync(
                        @"DELETE FROM dbo.management_userrole_table WHERE management_userrole_UserID = @ID;
                  DELETE FROM dbo.management_usercareer_table WHERE management_usercareer_UserID = @ID;
                  DELETE FROM dbo.management_user_table WHERE management_user_ID = @ID AND management_user_status = 0;",
                        new Dictionary<string, object> { { "@ID", request.Id } });

                    return Ok();
                }

                return BadRequest(new { error = "Tipo no soportado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inactive record");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocentesJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToDocente
                );

                var data = lista
                    .Where(d => d.TeacherId.HasValue)
                    .Select(d =>
                    {
                        var estado = string.IsNullOrWhiteSpace(d.Estado) ? "ACTIVO" : d.Estado.ToUpperInvariant();
                        var badgeClass = estado == "ACTIVO" ? "status-aceptado" : "status-rechazado";

                        return new
                        {
                            teacherId = d.TeacherId,
                            userId = d.UserId,
                            nombreCompleto = $"{d.ApellidoPaterno} {d.ApellidoMaterno}, {d.Nombre}".Trim(),
                            numeroEmpleado = d.NumeroEmpleado,
                            email = d.Email,
                            telefono = d.Telefono,
                            estado,
                            badgeClass
                        };
                    });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDocentesJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CreateTeacher(string? tab = null)
        {
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-docentes" : tab;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-docentes" : returnTab;

            if (ModelState.IsValid)
            {
                try
                {
                    var personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
                    {
                        { "@FirstName", model.FirstName },
                        { "@LastNamePaternal", model.LastNamePaternal },
                        { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                        { "@Email", (object?)model.Email ?? DBNull.Value },
                        { "@Phone", (object?)model.Phone ?? DBNull.Value },
                        { "@CURP", DBNull.Value }
                    });

                    await _repo.CreateTeacherAsync(new Dictionary<string, object>
                    {
                        { "@TeacherPersonID", personId },
                        { "@EmployeeNumber", (object?)model.EmployeeNumber ?? DBNull.Value },
                        { "@Teacher_StatusCode", string.IsNullOrWhiteSpace(model.StatusCode) ? "ACTIVO" : model.StatusCode }
                    });

                    if (!string.IsNullOrWhiteSpace(model.Username) && !string.IsNullOrWhiteSpace(model.Password))
                    {
                        var userId = await _repo.CreateUserAsync(new Dictionary<string, object>
                        {
                            { "@UserPersonID", personId },
                            { "@Username", model.Username },
                            { "@UserEmail", (object?)model.Email ?? DBNull.Value },
                            { "@PasswordHash", HashPassword(model.Password) }
                        });

                        var roles = await _repo.GetRolesAsync();
                        var teacherRoleId = roles.FirstOrDefault(r =>
                            r.Value.Contains("TEACHER", StringComparison.OrdinalIgnoreCase) ||
                            r.Value.Contains("DOCENTE", StringComparison.OrdinalIgnoreCase)).Key;

                        if (teacherRoleId > 0)
                        {
                            await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                            {
                                { "@UserRole_UserID", userId },
                                { "@UserRole_RoleID", teacherRoleId }
                            });
                        }
                    }

                    TempData["SuccessMessage"] = "Profesor registrado correctamente.";
                    return RedirectToAction("Catalogos", new { tab = returnTab });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating teacher");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = returnTab;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTeacher(int id, string? tab = null)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToDocente
            );

            var docente = result.FirstOrDefault(d => d.TeacherId == id);

            if (docente == null)
                return NotFound();

            var model = new CreateTeacherViewModel
            {
                TeacherId = docente.TeacherId,
                PersonId = docente.PersonId,
                FirstName = docente.Nombre,
                LastNamePaternal = docente.ApellidoPaterno,
                LastNameMaternal = docente.ApellidoMaterno,
                Email = docente.Email,
                Phone = docente.Telefono,
                EmployeeNumber = docente.NumeroEmpleado,
                StatusCode = string.IsNullOrWhiteSpace(docente.Estado) ? "ACTIVO" : docente.Estado
            };

            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-docentes" : tab;

            return View("CreateTeacher", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(CreateTeacherViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-docentes" : returnTab;

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                ViewBag.ReturnTab = returnTab;
                return View("CreateTeacher", model);
            }

            try
            {
                await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
                {
                    { "@ID", model.PersonId },
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", (object?)model.Email ?? DBNull.Value },
                    { "@Phone", (object?)model.Phone ?? DBNull.Value }
                });

                await _repo.ExecuteNonQueryAsync("management_teacher_update", new Dictionary<string, object>
                {
                    { "@ID", model.TeacherId },
                    { "@EmployeeNumber", (object?)model.EmployeeNumber ?? DBNull.Value },
                    { "@Teacher_StatusCode", string.IsNullOrWhiteSpace(model.StatusCode) ? "ACTIVO" : model.StatusCode }
                });

                TempData["SuccessMessage"] = "Profesor actualizado correctamente.";
                return RedirectToAction("Catalogos", new { tab = returnTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher");
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = returnTab;
            return View("CreateTeacher", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            try
            {
                var info = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToUsuario
                );

                var data = info.FirstOrDefault(x => x.IdentificadorTipo == "DOCENTE" && x.RelatedEntityId == id);

                var docentes = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToDocente
                );

                var docente = docentes.FirstOrDefault(x => x.TeacherId == id);
                if (docente == null)
                    return NotFound();

                var estado = (docente.Estado ?? "").ToUpperInvariant();

                if (estado == "ACTIVO")
                {
                    await _repo.ExecuteNonQueryAsync("management_teacher_softdelete",
                        new Dictionary<string, object> { { "@ID", id } });

                    if (data != null && data.Id > 0)
                    {
                        await _repo.ExecuteNonQueryAsync("management_user_softdelete",
                            new Dictionary<string, object> { { "@ID", data.Id } });
                    }

                    return Ok(new { mode = "softdelete" });
                }

                if (data != null && data.Id > 0)
                {
                    await _repo.ExecuteCommandAsync(
                        @"DELETE FROM dbo.management_userrole_table WHERE management_userrole_UserID = @UserId;
                  DELETE FROM dbo.management_usercareer_table WHERE management_usercareer_UserID = @UserId;
                  DELETE FROM dbo.management_user_table WHERE management_user_ID = @UserId AND management_user_status = 0;",
                        new Dictionary<string, object> { { "@UserId", data.Id } });
                }

                await _repo.ExecuteCommandAsync(
                    @"DELETE FROM dbo.management_teacher_table
              WHERE management_teacher_ID = @ID
                AND management_teacher_status = 0;",
                    new Dictionary<string, object> { { "@ID", id } });

                return Ok(new { mode = "harddelete" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting teacher");
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult TeacherDetail(int id)
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAssignableStudents(int teacherId)
        {
            return Json(new List<object>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignStudentsToTeacher(int teacherId, List<int> enrollmentIds)
        {
            TempData["SuccessMessage"] = "Estudiantes asignados correctamente.";
            return RedirectToAction("TeacherDetail", new { id = teacherId });
        }

        [HttpGet]
        public async Task<IActionResult> GetGruposJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "management_group_get",
                    null,
                    ModelMappers.MapToGroup
                );

                var data = lista.Select(g => new
                {
                    id = g.Id,
                    nombre = string.IsNullOrWhiteSpace(g.Nombre) ? g.Codigo : $"{g.Codigo} - {g.Nombre}",
                    carrera = g.Carrera,
                    turno = g.Turno,
                    esActivo = g.EsActivo
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGruposJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateGroup(string? tab = null)
        {
            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-grupos" : tab;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-grupos" : returnTab;

            if (ModelState.IsValid)
            {
                try
                {
                    await _repo.CreateGroupAsync(new Dictionary<string, object>
                    {
                        { "@GroupCareerID", model.CareerId ?? (object)DBNull.Value },
                        { "@GroupCode", model.GroupCode },
                        { "@GroupName", model.GroupName },
                        { "@GroupShift", model.Shift }
                    });

                    TempData["SuccessMessage"] = "Grupo creado exitosamente.";
                    return RedirectToAction("Catalogos", new { tab = returnTab });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating group");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = returnTab;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int id, string? tab = null)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "management_group_get",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToGroup
            );

            var group = result.FirstOrDefault();

            if (group == null)
                return NotFound();

            var model = new CreateGroupViewModel
            {
                Id = group.Id,
                GroupCode = group.Codigo,
                GroupName = group.Nombre,
                CareerId = group.CareerId,
                Shift = group.Turno
            };

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-grupos" : tab;

            return View("CreateGroup", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(CreateGroupViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-grupos" : returnTab;

            if (!ModelState.IsValid)
            {
                ViewBag.Careers = await _repo.GetCareersAsync();
                ViewBag.IsEdit = true;
                ViewBag.ReturnTab = returnTab;
                return View("CreateGroup", model);
            }

            try
            {
                await _repo.ExecuteNonQueryAsync("management_group_update", new Dictionary<string, object>
                {
                    { "@ID", model.Id },
                    { "@GroupCareerID", model.CareerId },
                    { "@GroupCode", model.GroupCode },
                    { "@GroupName", model.GroupName },
                    { "@GroupShift", model.Shift }
                });

                TempData["SuccessMessage"] = "Grupo actualizado correctamente.";
                return RedirectToAction("Catalogos", new { tab = returnTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = returnTab;
            return View("CreateGroup", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                await _repo.ExecuteNonQueryAsync("management_group_softdelete",
                    new Dictionary<string, object> { { "@ID", id } });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCareersJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "management_career_get",
                    null,
                    r => new
                    {
                        Id = Management.GetValue<int>(r, "management_career_ID"),
                        Code = Management.GetValue<string>(r, "management_career_Code"),
                        Name = Management.GetValue<string>(r, "management_career_Name")
                    });

                var data = lista.Select(c => new
                {
                    id = c.Id,
                    codigo = c.Code,
                    nombre = c.Name,
                    estado = true
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en carreras");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult CreateCareer(string? tab = null)
        {
            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-career" : tab;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCareer(CreateCareerViewModel model, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-career" : returnTab;

            if (ModelState.IsValid)
            {
                try
                {
                    await _repo.CreateCareerAsync(new Dictionary<string, object>
                    {
                        { "@CareerCode", model.Code },
                        { "@CareerName", model.Name }
                    });

                    TempData["SuccessMessage"] = "Carrera creada correctamente.";
                    return RedirectToAction("Catalogos", new { tab = returnTab });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando carrera");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.IsEdit = false;
            ViewBag.ReturnTab = returnTab;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCareer(int id, string? tab = null)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "management_career_get",
                new Dictionary<string, object> { { "@ID", id } },
                r => new CreateCareerViewModel
                {
                    Code = Management.GetValue<string>(r, "management_career_Code") ?? string.Empty,
                    Name = Management.GetValue<string>(r, "management_career_Name") ?? string.Empty
                }
            );

            var career = result.FirstOrDefault();

            if (career == null)
                return NotFound();

            ViewBag.CareerId = id;
            ViewBag.IsEdit = true;
            ViewBag.ReturnTab = string.IsNullOrWhiteSpace(tab) ? "tab-career" : tab;

            return View("CreateCareer", career);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCareer(CreateCareerViewModel model, int id, string? returnTab = null)
        {
            returnTab = string.IsNullOrWhiteSpace(returnTab) ? "tab-career" : returnTab;

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                ViewBag.CareerId = id;
                ViewBag.ReturnTab = returnTab;
                return View("CreateCareer", model);
            }

            try
            {
                await _repo.ExecuteNonQueryAsync("management_career_update", new Dictionary<string, object>
                {
                    { "@ID", id },
                    { "@CareerCode", model.Code },
                    { "@CareerName", model.Name }
                });

                TempData["SuccessMessage"] = "Carrera actualizada correctamente.";
                return RedirectToAction("Catalogos", new { tab = returnTab });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando carrera");
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.IsEdit = true;
            ViewBag.CareerId = id;
            ViewBag.ReturnTab = returnTab;
            return View("CreateCareer", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCareer(int id)
        {
            try
            {
                await _repo.ExecuteNonQueryAsync("management_career_softdelete",
                    new Dictionary<string, object> { { "@ID", id } });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting career");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHistorial()
        {
            try
            {
                var lista = await _repo.GetBitacoraAsync();

                var data = lista.Select(x => new
                {
                    fechaFormateada = x.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    usuario = x.Usuario,
                    nombreCompleto = x.NombreCompleto,
                    accion = x.Accion,
                    esActivo = x.Accion == "ALTA"
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en bitácora");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCyclesJson()
        {
            var lista = await _repo.ExecuteStoredProcedureAsync(
                "management_cycle_get",
                null,
                ModelMappers.MapToCycle
            );

            var data = lista.Select(c => new
            {
                id = c.Id,
                nombre = c.Name,
                inicio = c.StartDate.ToString("dd MMM yyyy"),
                fin = c.EndDate.ToString("dd MMM yyyy"),
                estado = c.StatusCode
            });

            return Json(new { data });
        }

        [HttpGet]
        public IActionResult CreateCycle()
        {
            ViewBag.IsEdit = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCycle(CycleViewModel model)
        {
            if (model.StartDate >= model.EndDate)
            {
                ModelState.AddModelError("", "La fecha de inicio debe ser menor a la fecha de fin.");
            }

            if (model.StatusCode == "ACTIVO")
            {
                var ciclos = await _repo.ExecuteStoredProcedureAsync(
                    "management_cycle_get",
                    null,
                    ModelMappers.MapToCycle
                );

                bool existeActivo = ciclos.Any(c => c.StatusCode == "ACTIVO");

                if (existeActivo)
                {
                    ModelState.AddModelError("", "Ya existe un ciclo ACTIVO. Debe finalizarlo antes de crear otro.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _repo.CreateCycleAsync(new Dictionary<string, object>
                {
                    { "@CycleName", model.Name },
                    { "@StartDate", model.StartDate },
                    { "@EndDate", model.EndDate },
                    { "@StatusCode", model.StatusCode }
                });

                return RedirectToAction("Ciclos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }

        public IActionResult Asignaciones()
        {
            return View();
        }

        public IActionResult Grupos()
        {
            return View();
        }

        public IActionResult SeguimientoDualEstadias()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateCycle()
        {
            ViewBag.IsEdit = false;
            return View();
        }

        public IActionResult Reportes()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }


    public class ToggleUserStatusRequest
    {
        public int Id { get; set; }
        public bool Activar { get; set; }
    }

    public class DeleteInactiveRecordRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }
}