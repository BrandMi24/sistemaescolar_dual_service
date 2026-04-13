using ControlEscolar.Data;
using ControlEscolar.Models;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Services;
using ControlEscolar.ViewModels.OperationalTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Coordinador,ServiceLearningCoordinator,Director,Admin,Administrator,Master")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Management _repo;
        private readonly ILogger<CoordinadorController> _logger;
        private readonly IOperationalAuditService _auditService;

        public CoordinadorController(
            ApplicationDbContext context,
            ILogger<CoordinadorController> logger,
            IOperationalAuditService auditService)
        {
            _context = context;
            _repo = new Management(context);
            _logger = logger;
            _auditService = auditService;
        }
        // ==========================================
        // 1. DASHBOARD Y NAVEGACIÓN PRINCIPAL
        // ==========================================
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Ciclos()
        {
            return View();
        }

        public IActionResult Catalogos()
        {
            return View();
        }

        // ==========================================
        // 2. MÓDULO DE USUARIOS GENERALES
        // ==========================================
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

                var data = lista.Select(u => new
                {
                    id = u.Id,
                    identificador = u.Identificador,
                    nombreCompleto = $"{u.LastNamePaternal} {u.LastNameMaternal}, {u.FirstName}".Trim(),
                    roles = u.Roles,
                    carrera = u.Carrera,
                    correo = u.Correo,
                    estado = u.Estado
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = await _repo.GetRolesAsync();
            ViewBag.IsEdit = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 1. Eliminar roles del usuario
            var roles = await _repo.ExecuteStoredProcedureAsync(
                "management_userrole_get",
                null,
                r => new
                {
                    Id = Management.GetValue<int>(r, "management_userrole_ID"),
                    UserId = Management.GetValue<int>(r, "management_userrole_UserID")
                }
            );

            foreach (var r in roles.Where(x => x.UserId == id))
            {
                await _repo.ExecuteNonQueryAsync("management_userrole_softdelete",
                    new Dictionary<string, object> { { "@ID", r.Id } });
            }

            // 2. Soft delete usuario
            await _repo.ExecuteNonQueryAsync("management_user_softdelete",
                new Dictionary<string, object> { { "@ID", id } });

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            // 1. Obtener el estudiante con joins
            var student = await _repo.ExecuteStoredProcedureAsync(
            "getview_user_full",
            new Dictionary<string, object> { { "@ID", id } },
                r => new {
                    StudentId = Management.GetValue<int>(r, "management_student_ID"),
                    UserId = Management.GetValue<int?>(r, "management_user_ID")
                }
            );

            var data = student.FirstOrDefault(s => s.StudentId == id);

            int? userId = data?.UserId;

            // 2. Soft delete student
            await _repo.ExecuteNonQueryAsync("management_student_softdelete",
                new Dictionary<string, object> { { "@ID", id } });

            // 3. Soft delete user relacionado
            if (data.UserId.HasValue)
            {
                // 🔥 ELIMINAR ROLES PRIMERO
                var roles = await _repo.ExecuteStoredProcedureAsync(
                    "management_userrole_get",
                    null,
                    r => new {
                        Id = Management.GetValue<int>(r, "management_userrole_ID"),
                        UserId = Management.GetValue<int>(r, "management_userrole_UserID")
                    }
                );

                foreach (var r in roles.Where(x => x.UserId == data.UserId))
                {
                    await _repo.ExecuteNonQueryAsync("management_userrole_softdelete",
                        new Dictionary<string, object> { { "@ID", r.Id } });
                }

                // Luego borrar usuario
                await _repo.ExecuteNonQueryAsync("management_user_softdelete",
                    new Dictionary<string, object> { { "@ID", data.UserId } });
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            // 1. Obtener docente correcto
            var teacher = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                new Dictionary<string, object> { { "@ID", id } },
                r => new {
                    TeacherId = Management.GetValue<int?>(r, "teacher_ID"),
                    UserId = Management.GetValue<int?>(r, "management_user_ID")
                }
            );

            var data = teacher.FirstOrDefault(t => t.TeacherId == id);

            if (data == null)
                return NotFound();

            // 2. Soft delete teacher
            await _repo.ExecuteNonQueryAsync("management_teacher_softdelete",
                new Dictionary<string, object> { { "@ID", id } });

            // 3. Soft delete user relacionado
            if (data.UserId.HasValue)
            {
                await _repo.ExecuteNonQueryAsync("management_user_softdelete",
                    new Dictionary<string, object> { { "@ID", data.UserId } });
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            await _repo.ExecuteNonQueryAsync("management_group_softdelete",
                new Dictionary<string, object> { { "@ID", id } });
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCareer(int id)
        {
            await _repo.ExecuteNonQueryAsync("management_career_softdelete",
                new Dictionary<string, object> { { "@ID", id } });

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Crear Persona
                    var personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
                    {
                        { "@FirstName", model.FirstName },
                        { "@LastNamePaternal", model.LastNamePaternal },
                        { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                        { "@Email", model.Email },
                        { "@Phone", DBNull.Value },
                        { "@CURP", DBNull.Value }
                    });

                    // 2. Crear Usuario
                    var roles = await _repo.GetRolesAsync();
                    if (!int.TryParse(model.Role, out int roleId))
                    {
                        ModelState.AddModelError("Role", "Rol inválido");
                        return View(model);
                    }

                    var userId = await _repo.CreateUserAsync(new Dictionary<string, object>
                    {
                        { "@UserPersonID", personId },
                        { "@Username", model.Username },
                        { "@UserEmail", model.Email },
                        { "@PasswordHash", HashPassword(model.Password) }
                    });

                    // 3. Asignar Rol
                    await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                    {
                        { "@UserRole_UserID", userId },
                        { "@UserRole_RoleID", roleId }
                    });

                    TempData["SuccessMessage"] = "Usuario creado correctamente.";
                    return RedirectToAction("Catalogos");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Error al procesar la solicitud: " + ex.Message);
                }
            }

            ViewBag.Roles = await _repo.GetRolesAsync();
            TempData["ErrorMessage"] = "Error al crear el usuario. Verifique los datos.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToUsuario
            );

            var user = result.FirstOrDefault();

            if (user == null)
                return NotFound();

            // Obtener roles
            var roles = await _repo.GetRolesAsync();

            // Convertir nombre → ID
            var roleId = roles.FirstOrDefault(r => user.Roles.Contains(r.Value)).Key;

            var model = new CreateUserViewModel
            {
                UserId = user.Id,
                PersonId = user.PersonId,
                FirstName = user.FirstName,
                LastNamePaternal = user.LastNamePaternal,
                LastNameMaternal = user.LastNameMaternal,
                Email = user.Correo,
                Username = user.Username,
                Role = roleId.ToString()
            };

            ViewBag.Roles = roles;
            ViewBag.IsEdit = true;

            return View("CreateUser", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(CreateUserViewModel model)
        {
            if (model.UserId == null || model.PersonId == null)
            {
                throw new Exception("IDs no recibidos correctamente");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _repo.GetRolesAsync();
                ViewBag.IsEdit = true;
                return View("CreateUser", model);
            }

            try
            {
                // 1. PERSON
                await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
                {
                    { "@ID", model.PersonId },
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", model.Email }
                });

                // 2. USER + PASSWORD
                await _repo.ExecuteNonQueryAsync("management_user_update", new Dictionary<string, object>
                {
                    { "@ID", model.UserId },
                    { "@Username", model.Username },
                    { "@UserEmail", model.Email },
                    { "@PasswordHash", string.IsNullOrWhiteSpace(model.Password)
                        ? DBNull.Value
                        : HashPassword(model.Password) }
                });

                // 4. ROLE
                if (int.TryParse(model.Role, out int roleId))
                {
                    // 1. Eliminar todos los roles actuales del usuario
                    var rolesActuales = await _repo.ExecuteStoredProcedureAsync(
                        "management_userrole_get",
                        null,
                        r => new
                        {
                            Id = Management.GetValue<int>(r, "management_userrole_ID"),
                            UserId = Management.GetValue<int>(r, "management_userrole_UserID")
                        }
                    );

                    foreach (var r in rolesActuales.Where(x => x.UserId == model.UserId))
                    {
                        await _repo.ExecuteNonQueryAsync("management_userrole_softdelete",
                            new Dictionary<string, object> { { "@ID", r.Id } });
                    }

                    // 2. Insertar nuevo rol
                    await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                    {
                        { "@UserRole_UserID", model.UserId },
                        { "@UserRole_RoleID", roleId }
                    });
                }

                return RedirectToAction("Catalogos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewBag.Roles = await _repo.GetRolesAsync();
            ViewBag.IsEdit = true;
            return View("CreateUser", model);
        }

        // ==========================================
        // 3. MÓDULO DE ALUMNOS
        // ==========================================

        /// <summary>Alumnos activos con matrícula asignada (tablaAlumnos).</summary>
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
                        { "@Student_IsFolio", 0 }
                    },
                    ModelMappers.MapToStudent);

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
                    estadoBadgeClass = s.EstadoCodigo == "INSCRITO" ? "status-aceptado"
                     : s.EstadoCodigo == "BAJA" ? "status-rechazado"
                     : "badge bg-secondary"
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
        public async Task<IActionResult> EditStudent(int id)
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
                Email = student.Email,
                CURP = student.CURP,
                Phone = student.Phone,
                CareerId = student.CareerId,
                GroupId = student.GroupId,
                StatusCode = student.EstadoCodigo
            };

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);

            ViewBag.StudentId = student.Id;
            ViewBag.IsEdit = true;

            return View("CreateStudent", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(CreateStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Careers = await _repo.GetCareersAsync();
                ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync(
                    "management_group_get", null, ModelMappers.MapToGroup
                );
                ViewBag.IsEdit = true;

                return View("CreateStudent", model);
            }

            try
            {
                // ==========================================
                // 1. OBTENER DATOS ANTERIORES
                // ==========================================
                var oldStudentResult = await _repo.ExecuteStoredProcedureAsync(
                    "getview_student_full",
                    new Dictionary<string, object> { { "@ID", model.StudentId } },
                    ModelMappers.MapToStudent
                );

                var old = oldStudentResult.FirstOrDefault();

                // ==========================================
                // 2. DETECTAR CAMBIOS
                // ==========================================
                bool cambioCarrera = old != null && old.CareerId != model.CareerId;
                bool cambioGrupo = old != null && old.GroupId != model.GroupId;

                // ==========================================
                // 3. GUARDAR HISTORIAL
                // ==========================================
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

                // ==========================================
                // 4. ACTUALIZAR PERSONA
                // ==========================================
                await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
        {
            { "@ID", model.PersonId },
            { "@FirstName", model.FirstName },
            { "@LastNamePaternal", model.LastNamePaternal },
            { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
            { "@Email", model.Email },
            { "@Phone", (object?)model.Phone ?? DBNull.Value },
            { "@CURP", model.CURP }
        });

                // ==========================================
                // 5. ACTUALIZAR STUDENT
                // ==========================================
                await _repo.ExecuteNonQueryAsync("management_student_update", new Dictionary<string, object>
        {
            { "@ID", model.StudentId },
            { "@StudentCareerID", model.CareerId },
            { "@StudentGroupID", model.GroupId ?? (object)DBNull.Value },
            { "@Student_StatusCode", model.StatusCode }
        });

                return RedirectToAction("Catalogos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.Groups = await _repo.ExecuteStoredProcedureAsync(
                "management_group_get", null, ModelMappers.MapToGroup
            );
            ViewBag.IsEdit = true;

            return View("CreateStudent", model);
        }

        /// <summary>Histórico de bajas: alumnos inactivos (tablaBajas).</summary>
        [HttpGet]
        public async Task<IActionResult> GetBajasJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    new Dictionary<string, object>
                    {
                        { "@Status", 0 }
                    },
                    ModelMappers.MapToUsuario
                );

                var data = lista.Select(u => new
                {
                    id = u.Id,
                    identificador = u.Identificador,
                    nombreCompleto = $"{u.LastNamePaternal} {u.LastNameMaternal}, {u.FirstName}".Trim(),
                    carrera = u.Carrera,
                    email = u.Correo,
                    estadoCodigo = u.Estado,
                    esActivo = false,
                    estadoBadgeClass = "status-rechazado"
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBajasJson");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> StudentDetail(int id)
        {
            var student = await _context.StudentsOperational
                .AsNoTracking()
                .Include(x => x.Person)
                .Include(x => x.Career)
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (student == null)
                return NotFound();

            var assignments = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Program)
                .Include(x => x.Teacher)
                    .ThenInclude(t => t!.Person)
                .Where(x => x.Status && x.StudentId == id)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var documents = await _context.OperationalDocuments
                .AsNoTracking()
                .Include(x => x.Assignment)
                .Where(x => x.Status && x.Assignment.StudentId == id)
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            var approvedHours = assignments.Sum(x => x.ApprovedHours);
            var requiredHours = assignments.Sum(x => (decimal)x.Program.RequiredHours);
            var pendingHours = Math.Max(0m, requiredHours - approvedHours);

            var vm = new CoordinadorStudentDetailViewModel
            {
                StudentId = student.Id,
                FullName = student.Person.FullName,
                Matricula = string.IsNullOrWhiteSpace(student.Matricula) ? "S/N" : student.Matricula,
                Email = string.IsNullOrWhiteSpace(student.Person.Email) ? "Sin correo" : student.Person.Email,
                Phone = string.IsNullOrWhiteSpace(student.Person.Phone) ? "Sin telefono" : student.Person.Phone,
                StatusCode = string.IsNullOrWhiteSpace(student.StatusCode) ? "N/A" : student.StatusCode,
                IsActive = student.Status,
                Curp = string.IsNullOrWhiteSpace(student.Person.CURP) ? "N/D" : student.Person.CURP,
                CareerName = student.Career?.Name ?? "Sin carrera",
                GroupName = string.IsNullOrWhiteSpace(student.Group?.Code) ? "Sin grupo" : student.Group.Code,
                ApprovedHours = approvedHours,
                PendingHours = pendingHours,
                DocumentsCount = documents.Count,
                EvaluationsCount = assignments.Count(x => x.EvaluationScore.HasValue),
                Assignments = assignments.Select(x => new CoordinadorStudentAssignmentRowViewModel
                {
                    AssignmentId = x.Id,
                    ProgramName = x.Program.Name,
                    ProgramType = x.Program.Type,
                    TeacherName = x.Teacher?.Person?.FullName ?? "Sin asignar",
                    StatusCode = x.StatusCode,
                    ApprovedHours = x.ApprovedHours,
                    RequiredHours = x.Program.RequiredHours,
                    ProgressPercent = x.Program.RequiredHours > 0
                        ? Math.Round((x.ApprovedHours / x.Program.RequiredHours) * 100m, 1)
                        : 0m,
                    EvaluationScore = x.EvaluationScore,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                Documents = documents.Take(20).Select(x => new CoordinadorStudentDocumentRowViewModel
                {
                    Title = x.Title,
                    DocumentType = x.DocumentType,
                    StatusCode = x.StatusCode,
                    UploadDate = x.UploadDate
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStudent()
        {
            ViewBag.Careers = await _repo.GetCareersAsync();
            var grupos = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.Groups = grupos;
            ViewBag.IsEdit = false;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Crear Persona
                    var personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
                    {
                        { "@FirstName", model.FirstName },
                        { "@LastNamePaternal", model.LastNamePaternal },
                        { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                        { "@Email", model.Email },
                        { "@Phone", (object?)model.Phone ?? DBNull.Value },
                        { "@CURP", model.CURP }
                    });

                    // 2. Crear Estudiante
                    var isFolio = model.StatusCode == "PREINSCRITO" ? 1 : 0;

                    var studentId = await _repo.CreateStudentAsync(new Dictionary<string, object>
                    {
                        { "@StudentPersonID", personId },
                        { "@StudentCareerID", model.CareerId },
                        { "@StudentGroupID", model.GroupId > 0 ? model.GroupId : DBNull.Value },
                        { "@Student_StatusCode", model.StatusCode },
                        { "@Student_Matricula", (object?)model.Matricula ?? DBNull.Value },
                        { "@Student_IsFolio", isFolio }
                    });

                    // ==========================================
                    // HISTORIAL INICIAL
                    // ==========================================

                    // Historial de carrera
                    await _repo.ExecuteNonQueryAsync("management_studentcareer_history_insert",
                        new Dictionary<string, object>
                        {
                            { "@StudentCareerHistory_StudentID", studentId },
                            { "@StudentCareerHistory_CareerID", model.CareerId },
                            { "@StudentCareerHistory_StartDate", DateTime.Now },
                            { "@StudentCareerHistory_EndDate", DBNull.Value },
                            { "@StudentCareerHistory_Reason", "Ingreso inicial" }
                        });

                    // Historial de grupo (solo si tiene)
                    if (model.GroupId != null && model.GroupId > 0)
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

                    // 3. Crear Usuario
                    {
                        // VALIDACIÓN
                        if (!string.IsNullOrWhiteSpace(model.Username) &&
                        !string.IsNullOrWhiteSpace(model.Password))
                        {
                            var userId = await _repo.CreateUserAsync(new Dictionary<string, object>
                            {
                                { "@UserPersonID", personId },
                                { "@Username", model.Username },
                                { "@UserEmail", model.Email },
                                { "@PasswordHash", HashPassword(model.Password) }
                            });

                            var studentRoleId = 2;

                            await _repo.CreateUserRoleAsync(new Dictionary<string, object>
                            {
                                { "@UserRole_UserID", userId },
                                { "@UserRole_RoleID", studentRoleId }
                            });
                        }
                    }

                    TempData["SuccessMessage"] = "Estudiante registrado correctamente.";
                    return RedirectToAction("Catalogos");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            var gruposReload = await _repo.ExecuteStoredProcedureAsync("management_group_get", null, ModelMappers.MapToGroup);
            ViewBag.Groups = gruposReload;
            TempData["ErrorMessage"] = "Hubo un error al registrar el estudiante.";
            return View(model);
        }

        // ==========================================
        // 4. MÓDULO DE PROFESORES
        // ==========================================

        /// <summary>Docentes activos (tablaDocentes).</summary>
        [HttpGet]
        public async Task<IActionResult> GetDocentesJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "getview_user_full",
                    null,
                    ModelMappers.MapToDocente);

                // Solo filas cuyo teacher_ID esté presente (son docentes)
                var data = lista
                    .Where(d => d.TeacherId.HasValue)
                    .Select(d => new
                    {
                        teacherId = d.TeacherId,
                        userId         = d.UserId,
                        nombreCompleto = $"{d.ApellidoPaterno} {d.ApellidoMaterno}, {d.Nombre}".Trim(),
                        numeroEmpleado = d.NumeroEmpleado,
                        email          = d.Email,
                        telefono       = d.Telefono,
                        estado         = string.IsNullOrWhiteSpace(d.Estado) ? "ACTIVO" : d.Estado,
                        badgeClass     = (d.Estado == "ACTIVO" || string.IsNullOrWhiteSpace(d.Estado))
                                            ? "status-aceptado" : "status-rechazado"
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
        public IActionResult CreateTeacher()
        {
            ViewBag.IsEdit = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Crear Persona
                    var personId = await _repo.CreatePersonAsync(new Dictionary<string, object>
            {
                { "@FirstName", model.FirstName },
                { "@LastNamePaternal", model.LastNamePaternal },
                { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                { "@Email", (object?)model.Email ?? DBNull.Value },
                { "@Phone", (object?)model.Phone ?? DBNull.Value },
                { "@CURP", DBNull.Value }
            });

                    // 2. Crear Profesor
                    await _repo.CreateTeacherAsync(new Dictionary<string, object>
            {
                { "@TeacherPersonID", personId },
                { "@EmployeeNumber", (object?)model.EmployeeNumber ?? DBNull.Value },
                { "@Teacher_StatusCode", string.IsNullOrWhiteSpace(model.StatusCode) ? "ACTIVO" : model.StatusCode }
            });

                    // 3. Crear Usuario
                    if (!string.IsNullOrWhiteSpace(model.Username) &&
                        !string.IsNullOrWhiteSpace(model.Password))
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
                            r.Value.Contains("DOCENTE", StringComparison.OrdinalIgnoreCase)
                        ).Key;

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
                    return RedirectToAction("Catalogos");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating teacher");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            TempData["ErrorMessage"] = "Hubo un error al registrar el profesor. Verifique los datos.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTeacher(int id)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "getview_user_full",
                new Dictionary<string, object> { { "@ID", id } },
                ModelMappers.MapToDocente
            );

            var docente = result.FirstOrDefault(d => d.TeacherId.HasValue);

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
                StatusCode = docente.Estado
            };

            ViewBag.IsEdit = true;

            return View("CreateTeacher", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(CreateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                return View("CreateTeacher", model);
            }

            try
            {
                // 1. Actualizar PERSONA
                await _repo.ExecuteNonQueryAsync("management_person_update", new Dictionary<string, object>
                {
                    { "@ID", model.PersonId },
                    { "@FirstName", model.FirstName },
                    { "@LastNamePaternal", model.LastNamePaternal },
                    { "@LastNameMaternal", (object?)model.LastNameMaternal ?? DBNull.Value },
                    { "@Email", (object?)model.Email ?? DBNull.Value },
                    { "@Phone", (object?)model.Phone ?? DBNull.Value }
                });

                // 2. Actualizar TEACHER
                await _repo.ExecuteNonQueryAsync("management_teacher_update", new Dictionary<string, object>
                {
                    { "@ID", model.TeacherId },
                    { "@EmployeeNumber", (object?)model.EmployeeNumber ?? DBNull.Value },
                    { "@Teacher_StatusCode", model.StatusCode }
                });

                TempData["SuccessMessage"] = "Profesor actualizado correctamente.";
                return RedirectToAction("Catalogos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher");
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            ViewBag.IsEdit = true;
            return View("CreateTeacher", model);
        }

        // ==========================================
        // 5. MÓDULO DE GRUPOS
        // ==========================================

        /// <summary>Todos los grupos (tablaGrupos).</summary>
        [HttpGet]
        public async Task<IActionResult> GetGruposJson()
        {
            try
            {
                var lista = await _repo.ExecuteStoredProcedureAsync(
                    "management_group_get",
                    null,
                    ModelMappers.MapToGroup);

                var data = lista.Select(g => new
                {
                    id          = g.Id,
                    nombre      = string.IsNullOrWhiteSpace(g.Nombre)
                                    ? g.Codigo
                                    : $"{g.Codigo} - {g.Nombre}",
                    carrera = g.Carrera,
                    turno       = g.Turno,
                    esActivo    = g.EsActivo
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
        public async Task<IActionResult> TeacherDetail(int id)
        {
            var teacher = await _context.TeachersOperational
                .AsNoTracking()
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (teacher == null)
                return NotFound();

            var assignments = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student)
                    .ThenInclude(s => s.Person)
                .Include(x => x.Student)
                    .ThenInclude(s => s.Career)
                .Include(x => x.Program)
                .Where(x => x.Status && x.TeacherId == id)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var assignmentIds = assignments.Select(x => x.Id).ToList();

            var pendingDocs = await _context.OperationalDocuments
                .AsNoTracking()
                .CountAsync(x => x.Status
                    && assignmentIds.Contains(x.AssignmentId)
                    && x.StatusCode == DocumentStatusCodes.PENDING);

            var totalStudents = assignments.Count;
            var activeStudents = assignments.Count(x => !string.Equals(x.StatusCode, "COMPLETED", StringComparison.OrdinalIgnoreCase));
            var completedStudents = assignments.Count(x => x.Program.RequiredHours > 0 && x.ApprovedHours >= x.Program.RequiredHours);
            var completionRate = totalStudents > 0 ? Math.Round((decimal)completedStudents / totalStudents * 100m, 1) : 0m;
            var pendingHours = assignments.Sum(x => Math.Max(0m, x.Program.RequiredHours - x.ApprovedHours));

            var vm = new CoordinadorTeacherDetailViewModel
            {
                TeacherId = teacher.Id,
                FullName = teacher.Person.FullName,
                EmployeeNumber = string.IsNullOrWhiteSpace(teacher.EmployeeNumber) ? "N/D" : teacher.EmployeeNumber,
                Email = string.IsNullOrWhiteSpace(teacher.Person.Email) ? "Sin correo" : teacher.Person.Email,
                Phone = string.IsNullOrWhiteSpace(teacher.Person.Phone) ? "Sin telefono" : teacher.Person.Phone,
                StatusCode = string.IsNullOrWhiteSpace(teacher.StatusCode) ? "N/A" : teacher.StatusCode,
                IsActive = teacher.Status,
                TotalStudents = totalStudents,
                ActiveStudents = activeStudents,
                PendingHours = pendingHours,
                PendingDocuments = pendingDocs,
                EvaluationsCount = assignments.Count(x => x.EvaluationScore.HasValue),
                CompletionRate = completionRate,
                AssignedStudents = assignments.Select(x => new CoordinadorTeacherStudentRowViewModel
                {
                    AssignmentId = x.Id,
                    StudentId = x.StudentId,
                    StudentName = x.Student.Person.FullName,
                    Matricula = string.IsNullOrWhiteSpace(x.Student.Matricula) ? "S/N" : x.Student.Matricula,
                    CareerName = x.Student.Career?.Name ?? "Sin carrera",
                    ProgramName = x.Program.Name,
                    ProgramType = x.Program.Type,
                    StatusCode = x.StatusCode,
                    ApprovedHours = x.ApprovedHours,
                    RequiredHours = x.Program.RequiredHours,
                    ProgressPercent = x.Program.RequiredHours > 0
                        ? Math.Round((x.ApprovedHours / x.Program.RequiredHours) * 100m, 1)
                        : 0m,
                }).ToList()
            };

            return View(vm);
        }

        // --- Endpoints para el Modal de Asignación en TeacherDetail ---
        [HttpGet]
        [Authorize(Roles = "Coordinador,ServiceLearningCoordinator,Admin,Administrator")]
        public async Task<IActionResult> GetAssignableStudents(int teacherId)
        {
            var assignments = await _context.OperationalStudentAssignments
                .Include(x => x.Student)
                    .ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Where(x => x.Status
                    && x.Program.Type == ProgramTypes.PRACTICAS_PROFESIONALES
                    && (x.TeacherId == null || x.TeacherId == teacherId))
                .OrderByDescending(x => x.CreatedDate)
                .Take(200)
                .ToListAsync();

            var data = assignments.Select(x => new
            {
                id = x.Id,
                nombre = x.Student.Person != null ? x.Student.Person.FullName : "Sin nombre",
                matricula = x.Student.Matricula,
                teacherId = x.TeacherId,
                estado = x.StatusCode
            });

            return Json(new { data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Coordinador,ServiceLearningCoordinator,Admin,Administrator")]
        public async Task<IActionResult> AssignStudentsToTeacher(int teacherId, List<int> enrollmentIds)
        {
            if (teacherId <= 0 || enrollmentIds == null || enrollmentIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Debes seleccionar docente y al menos una asignacion.";
                return RedirectToAction("TeacherDetail", new { id = teacherId });
            }

            var assignments = await _context.OperationalStudentAssignments
                .Where(x => enrollmentIds.Contains(x.Id))
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.TeacherId = teacherId;
                if (assignment.StatusCode == DualStatusCodes.LETTER_REQUESTED || assignment.StatusCode == DualStatusCodes.PLACEMENT)
                {
                    assignment.StatusCode = DualStatusCodes.ADVISORS_ASSIGNED;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("CoordinatorAction AssignStudentsToTeacher TeacherId={TeacherId} AssignmentCount={AssignmentCount}", teacherId, assignments.Count);
            await _auditService.LogAsync(
                module: "DUAL",
                action: "AssignStudentsToTeacher",
                entityName: "OperationalStudentAssignment",
                entityId: null,
                details: $"TeacherId={teacherId};AssignmentCount={assignments.Count}");
            TempData["SuccessMessage"] = "Estudiantes asignados correctamente.";
            return RedirectToAction("TeacherDetail", new { id = teacherId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Coordinador,ServiceLearningCoordinator,Admin,Administrator")]
        public async Task<IActionResult> ReassignSupervisor(int enrollmentId, int newTeacherId)
        {
            if (enrollmentId <= 0 || newTeacherId <= 0)
            {
                TempData["ErrorMessage"] = "Parametros de reasignacion invalidos.";
                return RedirectToAction(nameof(SeguimientoDualEstadias));
            }

            var assignment = await _context.OperationalStudentAssignments
                .FirstOrDefaultAsync(x => x.Id == enrollmentId && x.Status);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "No se encontro la asignacion indicada.";
                return RedirectToAction(nameof(SeguimientoDualEstadias));
            }

            assignment.TeacherId = newTeacherId;
            if (assignment.StatusCode == DualStatusCodes.LETTER_REQUESTED || assignment.StatusCode == DualStatusCodes.PLACEMENT)
            {
                assignment.StatusCode = DualStatusCodes.ADVISORS_ASSIGNED;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("CoordinatorAction ReassignSupervisor AssignmentId={AssignmentId} NewTeacherId={TeacherId}", enrollmentId, newTeacherId);
            await _auditService.LogAsync(
                module: "DUAL",
                action: "ReassignSupervisor",
                entityName: "OperationalStudentAssignment",
                entityId: enrollmentId,
                details: $"NewTeacherId={newTeacherId}");
            TempData["SuccessMessage"] = "Supervisor reasignado correctamente.";
            return RedirectToAction(nameof(SeguimientoDualEstadias));
        }

        // ==========================================
        // 5. MÓDULO DE GRUPOS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreateGroup()
        {
            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!model.CareerId.HasValue || model.CareerId <= 0)
                    {
                        ModelState.AddModelError("", "Debe seleccionar una carrera válida.");
                        ViewBag.Careers = await _repo.GetCareersAsync();
                        ViewBag.IsEdit = false;
                        return View(model);
                    }

                    await _repo.CreateGroupAsync(new Dictionary<string, object>
            {
                { "@GroupCareerID", model.CareerId ?? (object)DBNull.Value },
                { "@GroupCode", model.GroupCode },
                { "@GroupName", model.GroupName },
                { "@GroupShift", model.Shift }
            });

                    TempData["SuccessMessage"] = "Grupo creado exitosamente.";
                    return RedirectToAction("Catalogos");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating group");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = false;

            TempData["ErrorMessage"] = "Error al crear el grupo.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditGroup(int id)
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
                CareerId = group.CareerId.GetValueOrDefault(),
                Shift = group.Turno
            };

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.GroupId = group.Id;
            ViewBag.IsEdit = true;

            return View("CreateGroup", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Careers = await _repo.GetCareersAsync();
                ViewBag.IsEdit = true;
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

                return RedirectToAction("Catalogos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.Careers = await _repo.GetCareersAsync();
            ViewBag.IsEdit = true;
            return View("CreateGroup", model);
        }

        // --- Helper de Hashing ---
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // ==========================================
        // 6. Carreas (AJAX para DataTable)
        // ==========================================

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
        public IActionResult CreateCareer()
        {
            ViewBag.IsEdit = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCareer(CreateCareerViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _repo.CreateCareerAsync(new Dictionary<string, object>
            {
                { "@CareerCode", model.Code },
                { "@CareerName", model.Name }
            });

                    return RedirectToAction("Catalogos");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando carrera");
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCareer(int id)
        {
            var result = await _repo.ExecuteStoredProcedureAsync(
                "management_career_get",
                new Dictionary<string, object> { { "@ID", id } },
                r => new CreateCareerViewModel
                {
                    Code = Management.GetValue<string>(r, "management_career_Code"),
                    Name = Management.GetValue<string>(r, "management_career_Name")
                }
            );

            var career = result.FirstOrDefault();

            if (career == null)
                return NotFound();

            ViewBag.CareerId = id;
            ViewBag.IsEdit = true;

            return View("CreateCareer", career);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCareer(CreateCareerViewModel model, int id)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
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

                return RedirectToAction("Catalogos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.IsEdit = true;
            return View("CreateCareer", model);
        }

        // ==========================================
        // 7. BITÁCORA DEL SISTEMA (AJAX para DataTable)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Coordinador,ServiceLearningCoordinator,Admin,Administrator")]
        public async Task<IActionResult> GetHistorial(DateTime? from, DateTime? to, string? module, string? action, string? user)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
IF OBJECT_ID(N'dbo.operational_audit_table', N'U') IS NULL
BEGIN
    SELECT
        CAST(NULL AS DATETIME) AS Fecha,
        CAST(NULL AS NVARCHAR(50)) AS Modulo,
        CAST(NULL AS NVARCHAR(80)) AS Accion,
        CAST(NULL AS NVARCHAR(80)) AS Entidad,
        CAST(NULL AS NVARCHAR(100)) AS Usuario,
        CAST(NULL AS NVARCHAR(80)) AS Rol,
        CAST(NULL AS NVARCHAR(500)) AS Detalle
    WHERE 1 = 0;
END
ELSE
BEGIN
    SELECT TOP 2000
        a.operational_audit_CreatedDate AS Fecha,
        a.operational_audit_Module AS Modulo,
        a.operational_audit_Action AS Accion,
        a.operational_audit_Entity AS Entidad,
        ISNULL(a.operational_audit_Username, CONCAT('USER#', ISNULL(CONVERT(varchar(20), a.operational_audit_UserID), 'N/A'))) AS Usuario,
        a.operational_audit_Role AS Rol,
        a.operational_audit_Details AS Detalle
    FROM dbo.operational_audit_table a
    WHERE a.operational_audit_status = 1
      AND (@From IS NULL OR a.operational_audit_CreatedDate >= @From)
      AND (@ToExclusive IS NULL OR a.operational_audit_CreatedDate < @ToExclusive)
      AND (@Module IS NULL OR a.operational_audit_Module = @Module)
      AND (@Action IS NULL OR a.operational_audit_Action = @Action)
      AND (@UserSearch IS NULL OR ISNULL(a.operational_audit_Username, '') LIKE '%' + @UserSearch + '%')
      AND (@IsAdmin = 1 OR a.operational_audit_Module IN ('DUAL', 'DUAL_SS'))
    ORDER BY a.operational_audit_CreatedDate DESC;
END";

                var toExclusive = to?.Date.AddDays(1);
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Administrator");

                cmd.Parameters.Add(new SqlParameter("@From", (object?)from?.Date ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ToExclusive", (object?)toExclusive ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Module", string.IsNullOrWhiteSpace(module) ? DBNull.Value : module.Trim()));
                cmd.Parameters.Add(new SqlParameter("@Action", string.IsNullOrWhiteSpace(action) ? DBNull.Value : action.Trim()));
                cmd.Parameters.Add(new SqlParameter("@UserSearch", string.IsNullOrWhiteSpace(user) ? DBNull.Value : user.Trim()));
                cmd.Parameters.Add(new SqlParameter("@IsAdmin", isAdmin));

                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                var data = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        fechaFormateada = reader.GetDateTime(reader.GetOrdinal("Fecha")).ToString("dd/MM/yyyy HH:mm"),
                        modulo = reader.GetString(reader.GetOrdinal("Modulo")),
                        accion = reader.GetString(reader.GetOrdinal("Accion")),
                        entidad = reader.GetString(reader.GetOrdinal("Entidad")),
                        usuario = reader.GetString(reader.GetOrdinal("Usuario")),
                        rol = reader.IsDBNull(reader.GetOrdinal("Rol")) ? string.Empty : reader.GetString(reader.GetOrdinal("Rol")),
                        detalle = reader.IsDBNull(reader.GetOrdinal("Detalle")) ? string.Empty : reader.GetString(reader.GetOrdinal("Detalle"))
                    });
                }

                await conn.CloseAsync();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en bitácora");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // 8. GESTIÓN OPERATIVA
        // ==========================================
        public async Task<IActionResult> Asignaciones(
            string? search,
            string? status,
            int? teacher,
            DateTime? createdFrom,
            DateTime? createdTo,
            string? sort = "created",
            string? order = "desc",
            int page = 1,
            int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize is 5 or 10 or 20 ? pageSize : 10;
            var isAsc = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

            var query = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student)
                    .ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Include(x => x.Teacher)
                    .ThenInclude(x => x.Person)
                .Where(x => x.Status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpper();
                query = query.Where(x =>
                    (x.Student.Matricula != null && x.Student.Matricula.ToUpper().Contains(term)) ||
                    (x.Student.Person != null &&
                     ((x.Student.Person.FirstName ?? "") + " " + (x.Student.Person.LastNamePaternal ?? "") + " " + (x.Student.Person.LastNameMaternal ?? "")).ToUpper().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.StatusCode == status);
            }

            if (teacher.HasValue)
            {
                query = query.Where(x => x.TeacherId == teacher.Value);
            }

            if (createdFrom.HasValue)
            {
                var from = createdFrom.Value.Date;
                query = query.Where(x => x.CreatedDate >= from);
            }

            if (createdTo.HasValue)
            {
                var toExclusive = createdTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedDate < toExclusive);
            }

            query = (sort ?? "created").ToLowerInvariant() switch
            {
                "student" => isAsc
                    ? query.OrderBy(x => x.Student.Person.FirstName).ThenBy(x => x.Student.Person.LastNamePaternal)
                    : query.OrderByDescending(x => x.Student.Person.FirstName).ThenByDescending(x => x.Student.Person.LastNamePaternal),
                "status" => isAsc
                    ? query.OrderBy(x => x.StatusCode)
                    : query.OrderByDescending(x => x.StatusCode),
                "hours" => isAsc
                    ? query.OrderBy(x => x.ApprovedHours)
                    : query.OrderByDescending(x => x.ApprovedHours),
                "program" => isAsc
                    ? query.OrderBy(x => x.Program.Name)
                    : query.OrderByDescending(x => x.Program.Name),
                _ => isAsc
                    ? query.OrderBy(x => x.CreatedDate)
                    : query.OrderByDescending(x => x.CreatedDate)
            };

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CoordinatorAssignmentRowViewModel
                {
                    AssignmentId = x.Id,
                    StudentName = x.Student.Person != null ? x.Student.Person.FullName : "Sin nombre",
                    Matricula = x.Student.Matricula,
                    ProgramName = x.Program.Name,
                    OrganizationName = x.Organization != null ? x.Organization.Name : "Sin organizacion",
                    TeacherId = x.TeacherId,
                    TeacherName = x.Teacher != null ? x.Teacher.Person.FullName : "SIN ASIGNAR",
                    StatusCode = x.StatusCode,
                    ApprovedHours = x.ApprovedHours,
                    RequiredHours = x.Program.RequiredHours
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Teacher = teacher;
            ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd");
            ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");
            ViewBag.Sort = sort;
            ViewBag.Order = isAsc ? "asc" : "desc";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.Teachers = await _context.TeachersOperational
                .AsNoTracking()
                .Include(x => x.Person)
                .Where(x => x.Status)
                .OrderBy(x => x.Person.FirstName)
                .Select(x => new { x.Id, Name = x.Person.FullName })
                .ToListAsync();

            return View(items);
        }

        public IActionResult Grupos()
        {
            return View();
        }

        public async Task<IActionResult> SeguimientoDualEstadias(
            string? search,
            string? status,
            int? teacher,
            DateTime? createdFrom,
            DateTime? createdTo,
            string? sort = "created",
            string? order = "desc",
            int page = 1,
            int pageSize = 10)
        {
            return await Asignaciones(search, status, teacher, createdFrom, createdTo, sort, order, page, pageSize);
        }

        // ==========================================
        // 9. ANALÍTICA
        // ==========================================
        public IActionResult Reportes()
        {
            return View();
        }
    }
}