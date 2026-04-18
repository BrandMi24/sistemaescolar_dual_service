using ClosedXML.Excel;
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

        // ─────────────────────────────────────────────────────────────────────
        // EXPORTAR EXCEL
        // GET: /VisitaMedica/ExportarExcel?fechaInicio=2026-01-01&fechaFin=2026-04-30
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var lista = new List<VisitaMedica>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                // Construimos el filtro de fechas sobre registros individuales
                // y agrupamos para mostrar la visita más reciente en el rango
                var condiciones = new List<string>();
                if (fechaInicio.HasValue)
                    condiciones.Add("v.FechaVisita >= @fechaInicio");
                if (fechaFin.HasValue)
                    condiciones.Add("v.FechaVisita < DATEADD(day, 1, @fechaFin)");

                var whereClause = condiciones.Count > 0
                    ? "WHERE " + string.Join(" AND ", condiciones)
                    : "";

                var query = $@"
                SELECT 
                    v.Matricula,
                    v.FechaVisita,
                    v.Diagnostico,
                    v.Edad,
                    v.Talla,
                    v.Peso,
                    v.PresionArterial,
                    v.FrecuenciaCardiaca,
                    v.Temperatura,
                    ISNULL(NULLIF(LTRIM(RTRIM(
                        dp.academiccontrol_preinscription_personaldata_name + ' ' +
                        dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' +
                        dp.academiccontrol_preinscription_personaldata_maternalSurname
                    )), ''), 'FALTA NOMBRE EN BD') AS NombreCompleto,
                    ISNULL(NULLIF(LTRIM(RTRIM(i.academiccontrol_inscription_careerRequested)), ''), 'FALTA CARRERA EN BD') AS Carrera
                FROM Visitas v
                LEFT JOIN academiccontrol_inscription_table i
                    ON v.Matricula = i.academiccontrol_inscription_enrollment
                LEFT JOIN academiccontrol_preinscription_personaldata_table dp
                    ON i.academiccontrol_inscription_preinscriptionID = dp.academiccontrol_preinscription_personaldata_preinscriptionID
                {whereClause}
                ORDER BY v.FechaVisita DESC";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    if (fechaInicio.HasValue)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = "@fechaInicio";
                        p.Value = fechaInicio.Value.Date;
                        command.Parameters.Add(p);
                    }
                    if (fechaFin.HasValue)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = "@fechaFin";
                        p.Value = fechaFin.Value.Date;
                        command.Parameters.Add(p);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new VisitaMedica
                            {
                                Matricula = reader["Matricula"].ToString() ?? "",
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["Carrera"].ToString(),
                                FechaVisita = (DateTime)reader["FechaVisita"],
                                Diagnostico = reader["Diagnostico"].ToString() ?? "",
                                Edad = reader["Edad"] != DBNull.Value ? Convert.ToInt32(reader["Edad"]) : 0,
                                Talla = reader["Talla"] != DBNull.Value ? Convert.ToDouble(reader["Talla"]) : null,
                                Peso = reader["Peso"] != DBNull.Value ? Convert.ToDouble(reader["Peso"]) : null,
                                PresionArterial = reader["PresionArterial"].ToString(),
                                FrecuenciaCardiaca = reader["FrecuenciaCardiaca"].ToString(),
                                Temperatura = reader["Temperatura"] != DBNull.Value ? Convert.ToDouble(reader["Temperatura"]) : null
                            });
                        }
                    }
                }
            }

            // ── Construir Excel ──────────────────────────────────────────────
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Visitas Médicas");

            // Encabezado con título
            ws.Cell(1, 1).Value = "Reporte de Visitas Médicas - UT Tamaulipas";
            ws.Range(1, 1, 1, 11).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1a3c34"))
                .Font.SetFontColor(XLColor.White);

            // Sub-encabezado con rango de fechas
            var rango = fechaInicio.HasValue || fechaFin.HasValue
                ? $"Período: {(fechaInicio.HasValue ? fechaInicio.Value.ToString("dd/MM/yyyy") : "inicio")} — {(fechaFin.HasValue ? fechaFin.Value.ToString("dd/MM/yyyy") : "hoy")}"
                : "Período: Todos los registros";
            ws.Cell(2, 1).Value = rango;
            ws.Range(2, 1, 2, 11).Merge().Style
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#d1e7dd"))
                .Font.SetFontColor(XLColor.FromHtml("#0a3622"));

            // Cabeceras de columnas
            var headers = new[] { "Nombre Completo", "Matrícula", "Carrera", "Fecha Visita",
                                   "Edad", "Talla (m)", "Peso (kg)", "Presión Arterial",
                                   "Frec. Cardíaca", "Temperatura (°C)", "Diagnóstico" };
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(3, col + 1);
                cell.Value = headers[col];
                cell.Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#495057"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetBottomBorder(XLBorderStyleValues.Thin);
            }

            // Datos
            int row = 4;
            foreach (var v in lista)
            {
                ws.Cell(row, 1).Value = v.NombreCompleto;
                ws.Cell(row, 2).Value = v.Matricula;
                ws.Cell(row, 3).Value = v.Carrera;
                ws.Cell(row, 4).Value = v.FechaVisita.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 5).Value = v.Edad;
                ws.Cell(row, 6).Value = v.Talla.HasValue ? v.Talla.Value.ToString("0.00") : "";
                ws.Cell(row, 7).Value = v.Peso.HasValue ? v.Peso.Value.ToString("0.00") : "";
                ws.Cell(row, 8).Value = v.PresionArterial ?? "";
                ws.Cell(row, 9).Value = v.FrecuenciaCardiaca ?? "";
                ws.Cell(row, 10).Value = v.Temperatura.HasValue ? v.Temperatura.Value.ToString("0.0") : "";
                ws.Cell(row, 11).Value = v.Diagnostico;

                // Alternar color de filas
                if (row % 2 == 0)
                    ws.Range(row, 1, row, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8f9fa"));

                row++;
            }

            // Total de registros al final
            ws.Cell(row + 1, 1).Value = $"Total de registros: {lista.Count}";
            ws.Range(row + 1, 1, row + 1, 4).Merge().Style
                .Font.SetBold(true)
                .Font.SetItalic(true);

            // Ajustar ancho de columnas automáticamente
            ws.Columns().AdjustToContents();

            // Congelar fila de cabecera
            ws.SheetView.FreezeRows(3);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var nombreArchivo = $"VisitasMedicas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }
    }
}