using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Data;
using ControlEscolar.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Physicologyst,Head Nurse")]
    public class VisitaPsicologicaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VisitaPsicologicaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VisitaPsicologica model)
        {
            ModelState.Remove("NombreCompleto");
            ModelState.Remove("Carrera");

            if (!ModelState.IsValid) return View(model);

            model.FechaVisita = DateTime.Now;
            _context.VisitasPsicologicas.Add(model);
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
            var lista = new List<VisitaPsicologica>();
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 
                    v.Matricula,
                    MAX(v.FechaVisita) AS FechaVisita,
                    ISNULL(NULLIF(LTRIM(RTRIM(dp.academiccontrol_preinscription_personaldata_name + ' ' + dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' + dp.academiccontrol_preinscription_personaldata_maternalSurname)), ''), 'FALTA NOMBRE EN BD') AS NombreCompleto,
                    ISNULL(NULLIF(LTRIM(RTRIM(i.academiccontrol_inscription_careerRequested)), ''), 'FALTA CARRERA EN BD') AS Carrera
                FROM VisitasPsicologicas v
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
                            lista.Add(new VisitaPsicologica
                            {
                                Matricula = reader["Matricula"].ToString() ?? string.Empty,
                                NombreCompleto = reader["NombreCompleto"].ToString() ?? string.Empty,
                                Carrera = reader["Carrera"].ToString() ?? string.Empty,
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
            var historial = await _context.VisitasPsicologicas.Where(v => v.Matricula == id).OrderByDescending(v => v.FechaVisita).ToListAsync();
            ViewData["Matricula"] = id;
            return View(historial);
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXPORTAR EXCEL
        // GET: /VisitaPsicologica/ExportarExcel?fechaInicio=2026-01-01&fechaFin=2026-04-30
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var lista = new List<VisitaPsicologica>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();

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
                    v.Edad,
                    v.MotivoConsulta,
                    v.TerapiaPrevia,
                    v.MotivoConsultaPrevia,
                    v.MedicacionPsiquiatrica,
                    ISNULL(NULLIF(LTRIM(RTRIM(
                        dp.academiccontrol_preinscription_personaldata_name + ' ' +
                        dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' +
                        dp.academiccontrol_preinscription_personaldata_maternalSurname
                    )), ''), 'FALTA NOMBRE EN BD') AS NombreCompleto,
                    ISNULL(NULLIF(LTRIM(RTRIM(i.academiccontrol_inscription_careerRequested)), ''), 'FALTA CARRERA EN BD') AS Carrera
                FROM VisitasPsicologicas v
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
                            lista.Add(new VisitaPsicologica
                            {
                                Matricula = reader["Matricula"].ToString() ?? "",
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["Carrera"].ToString(),
                                FechaVisita = (DateTime)reader["FechaVisita"],
                                Edad = reader["Edad"] != DBNull.Value ? Convert.ToInt32(reader["Edad"]) : 0,
                                MotivoConsulta = reader["MotivoConsulta"].ToString() ?? "",
                                TerapiaPrevia = reader["TerapiaPrevia"] != DBNull.Value && Convert.ToBoolean(reader["TerapiaPrevia"]),
                                MotivoConsultaPrevia = reader["MotivoConsultaPrevia"].ToString(),
                                MedicacionPsiquiatrica = reader["MedicacionPsiquiatrica"].ToString()
                            });
                        }
                    }
                }
            }

            // ── Construir Excel ──────────────────────────────────────────────
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Visitas Psicológicas");

            // Encabezado con título
            ws.Cell(1, 1).Value = "Reporte de Visitas Psicológicas - UT Tamaulipas";
            ws.Range(1, 1, 1, 9).Merge().Style
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
            ws.Range(2, 1, 2, 9).Merge().Style
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#d1e7dd"))
                .Font.SetFontColor(XLColor.FromHtml("#0a3622"));

            // Cabeceras
            var headers = new[] { "Nombre Completo", "Matrícula", "Carrera", "Fecha Sesión",
                                   "Edad", "Motivo de Consulta", "Terapia Previa",
                                   "Motivo Consulta Previa", "Medicación Psiquiátrica" };
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
                ws.Cell(row, 6).Value = v.MotivoConsulta;
                ws.Cell(row, 7).Value = v.TerapiaPrevia ? "Sí" : "No";
                ws.Cell(row, 8).Value = v.MotivoConsultaPrevia ?? "";
                ws.Cell(row, 9).Value = v.MedicacionPsiquiatrica ?? "";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, 9).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8f9fa"));

                row++;
            }

            // Total al final
            ws.Cell(row + 1, 1).Value = $"Total de registros: {lista.Count}";
            ws.Range(row + 1, 1, row + 1, 4).Merge().Style
                .Font.SetBold(true)
                .Font.SetItalic(true);

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(3);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var nombreArchivo = $"VisitasPsicologicas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }
    }
}