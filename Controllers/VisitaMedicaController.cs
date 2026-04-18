using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ControlEscolar.Controllers
{
    public class VisitaMedicaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VisitaMedicaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GestionUsuarios()
        {
            var viewModel = new UsuariosViewModel();
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
          SELECT
              u.management_user_ID AS Id,
              (p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal + ' ' + ISNULL(p.management_person_LastNameMaternal, '')) AS Nombre,
              u.management_user_Email AS Email,
              ISNULL(STRING_AGG(r.management_role_Name, ', ') WITHIN GROUP (ORDER BY r.management_role_Name), 'Sin Rol') AS Rol
          FROM management_user_table u
          INNER JOIN management_person_table p ON u.management_user_PersonID = p.management_person_ID
          LEFT JOIN management_userrole_table ur ON ur.management_userrole_UserID = u.management_user_ID AND ur.management_userrole_status = 1
          LEFT JOIN management_role_table r ON r.management_role_ID = ur.management_userrole_RoleID
          WHERE u.management_user_status = 1
          GROUP BY u.management_user_ID, p.management_person_FirstName, p.management_person_LastNamePaternal, p.management_person_LastNameMaternal, u.management_user_Email
          ORDER BY Nombre";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            viewModel.Users.Add(new UserDetalle
                            {
                                Id = (int)reader["Id"],
                                Nombre = reader["Nombre"].ToString(),
                                Email = reader["Email"].ToString(),
                                Rol = reader["Rol"].ToString()
                            });
                        }
                    }
                }
            }
            return View(viewModel);
        }

        [HttpPost]
       
        public async Task<IActionResult> AsignarRol([FromBody] AsignarRolRequest request)
        {
            if (request == null || request.UserId <= 0 || request.RolId <= 0)
                return Json(new { success = false, message = "Datos inválidos." });

            var rolesPermitidos = new[] { 4, 5, 6 };
            if (!rolesPermitidos.Contains(request.RolId))
                return Json(new { success = false, message = "Rol no permitido." });

            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    var queryDesactivar = @"
                  UPDATE management_userrole_table
                  SET management_userrole_status = 0
                  WHERE management_userrole_UserID = @userId
                    AND management_userrole_RoleID IN (4, 5, 6)";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = queryDesactivar;
                        var p = cmd.CreateParameter(); p.ParameterName = "@userId"; p.Value = request.UserId;
                        cmd.Parameters.Add(p);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    var queryCheck = @"
                  SELECT COUNT(1) FROM management_userrole_table
                  WHERE management_userrole_UserID = @userId AND management_userrole_RoleID = @rolId";

                    int existe = 0;
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = queryCheck;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@userId"; p1.Value = request.UserId; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@rolId"; p2.Value = request.RolId; cmd.Parameters.Add(p2);
                        existe = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    if (existe > 0)
                    {
                        var queryActivar = @"
                      UPDATE management_userrole_table
                      SET management_userrole_status = 1
                      WHERE management_userrole_UserID = @userId AND management_userrole_RoleID = @rolId";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = queryActivar;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@userId"; p1.Value = request.UserId; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@rolId"; p2.Value = request.RolId; cmd.Parameters.Add(p2);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        var queryInsertar = @"
                      INSERT INTO management_userrole_table 
                          (management_userrole_UserID, management_userrole_RoleID, management_userrole_status)
                      VALUES (@userId, @rolId, 1)";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = queryInsertar;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@userId"; p1.Value = request.UserId; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@rolId"; p2.Value = request.RolId; cmd.Parameters.Add(p2);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuitarRol([FromBody] QuitarRolRequest request)
        {
            if (request == null || request.UserId <= 0)
                return Json(new { success = false, message = "Datos inválidos." });

            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    var query = @"
                        UPDATE management_userrole_table
                        SET management_userrole_status = 0
                        WHERE management_userrole_UserID = @userId
                          AND management_userrole_RoleID IN (4, 5, 6)";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = query;
                        var p = cmd.CreateParameter(); p.ParameterName = "@userId"; p.Value = request.UserId;
                        cmd.Parameters.Add(p);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VisitaMedica model)
        {
            ModelState.Remove("NombreCompleto");
            ModelState.Remove("Carrera");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.FechaVisita = DateTime.Now;
            _context.Visitas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosAlumno(string matricula)
        {
            if (string.IsNullOrEmpty(matricula)) return BadRequest("Matrícula vacía");

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 
                    (dp.academiccontrol_preinscription_personaldata_name + ' ' + dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' + dp.academiccontrol_preinscription_personaldata_maternalSurname) AS NombreCompleto,
                    p.academiccontrol_preinscription_careerRequested AS Carrera,
                    CONVERT(varchar, dp.academiccontrol_preinscription_personaldata_birthDate, 23) AS FechaNacimiento
                FROM academiccontrol_inscription_table i
                INNER JOIN academiccontrol_preinscription_table p ON i.academiccontrol_inscription_preinscriptionID = p.academiccontrol_preinscription_ID
                INNER JOIN academiccontrol_preinscription_personaldata_table dp ON dp.academiccontrol_preinscription_personaldata_preinscriptionID = p.academiccontrol_preinscription_ID
                WHERE i.academiccontrol_inscription_enrollment = @matricula";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var param = command.CreateParameter();
                    param.ParameterName = "@matricula";
                    param.Value = matricula;
                    command.Parameters.Add(param);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Json(new
                            {
                                nombreCompleto = reader["NombreCompleto"].ToString(),
                                carrera = reader["Carrera"].ToString(),
                                fechaNacimiento = reader["FechaNacimiento"].ToString()
                            });
                        }
                    }
                }
            }
            return NotFound("No se encontró el alumno");
        }

        public async Task<IActionResult> Index()
        {
            var lista = new List<VisitaMedica>();
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 
                    v.Matricula,
                    MAX(v.FechaVisita) AS FechaVisita,
                    ISNULL(NULLIF(LTRIM(RTRIM(dp.academiccontrol_preinscription_personaldata_name + ' ' + dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' + dp.academiccontrol_preinscription_personaldata_maternalSurname)), ''), 'FALTA NOMBRE EN BD') AS NombreCompleto,
                    ISNULL(NULLIF(LTRIM(RTRIM(i.academiccontrol_inscription_careerRequested)), ''), 'FALTA CARRERA EN BD') AS Carrera
                FROM Visitas v
                LEFT JOIN academiccontrol_inscription_table i ON v.Matricula = i.academiccontrol_inscription_enrollment
                LEFT JOIN academiccontrol_preinscription_personaldata_table dp ON i.academiccontrol_inscription_preinscriptionID = dp.academiccontrol_preinscription_personaldata_preinscriptionID
                GROUP BY v.Matricula, dp.academiccontrol_preinscription_personaldata_name, dp.academiccontrol_preinscription_personaldata_paternalSurname, dp.academiccontrol_preinscription_personaldata_maternalSurname, i.academiccontrol_inscription_careerRequested";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new VisitaMedica
                            {
                                Matricula = reader["Matricula"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["Carrera"].ToString(),
                                FechaVisita = (DateTime)reader["FechaVisita"]
                            });
                        }
                    }
                }
            }
            return View(lista);
        }

        public async Task<IActionResult> History(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var historial = await _context.Visitas.Where(v => v.Matricula == id).OrderByDescending(v => v.FechaVisita).ToListAsync();
            ViewData["Matricula"] = id;
            return View(historial);
        }
    }
}