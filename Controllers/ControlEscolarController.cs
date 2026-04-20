using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Data;
using ControlEscolar.Enums;
using ControlEscolar.Models;
using ControlEscolar.Services;

namespace ControlEscolar.Controllers
{
    [Authorize]
    public class ControlEscolarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ControlEscolarController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string tab = "preinscripciones",
                                       string? carrera = null,
                                       string? estado = null)
        {

            if (!Request.Query.ContainsKey("estado") && estado == null)
                estado = "Pendiente";
            else
                estado = string.IsNullOrEmpty(estado) ? null : estado;

            var preinscripciones = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.DatosEscolares)
                .ToListAsync();

            var inscripciones = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .ToListAsync();

            if (!string.IsNullOrEmpty(carrera))
                preinscripciones = preinscripciones
                    .Where(p => p.academiccontrol_preinscription_careerRequested == carrera).ToList();

            if (!string.IsNullOrEmpty(estado))
                preinscripciones = preinscripciones
                    .Where(p => p.academiccontrol_preinscription_state == estado).ToList();

            if (!string.IsNullOrEmpty(carrera))
                inscripciones = inscripciones
                    .Where(i => i.academiccontrol_inscription_careerRequested == carrera).ToList();

            if (!string.IsNullOrEmpty(estado))
                inscripciones = inscripciones
                    .Where(i => i.academiccontrol_inscription_state == estado).ToList();

            var vm = new ControlEscolarViewModel
            {
                Tab = tab,
                Preinscripciones = preinscripciones,
                Inscripciones = inscripciones,
                FiltroCarrera = carrera,
                FiltroEstado = estado
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ValidarPreinscripcion(int id)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_preinscription_state = EstadoPreinscripcion.Validada.ToString();

            await _context.SaveChangesAsync();

            if (entidad.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Preinscripción Validada",
                    $"Estimado(a) {entidad.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"su preinscripción con folio {entidad.academiccontrol_preinscription_folio} ha sido validada correctamente."
                );
            }

            TempData["SuccessMessage"] = $"Preinscripción {entidad.academiccontrol_preinscription_folio} validada.";
            return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
        }

        [HttpPost]
        public async Task<IActionResult> HabilitarInscripcion(int id)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            if (entidad.academiccontrol_preinscription_state != EstadoPreinscripcion.Validada.ToString())
            {
                TempData["ErrorMessage"] = "Debe estar validada.";
                return RedirectToAction(nameof(Index));
            }

            entidad.academiccontrol_preinscription_state = EstadoPreinscripcion.InscripcionHabilitada.ToString();

            await _context.SaveChangesAsync();

            if (entidad.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Inscripción Habilitada",
                    $"Folio: {entidad.academiccontrol_preinscription_folio}"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RechazarPreinscripcion(int id, string? motivo)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_preinscription_state = EstadoPreinscripcion.Rechazada.ToString();
            entidad.academiccontrol_preinscription_rejectionReason = motivo;

            await _context.SaveChangesAsync();

            if (entidad.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Preinscripción Rechazada",
                    $"Estimado(a) {entidad.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"lamentablemente su preinscripción con folio {entidad.academiccontrol_preinscription_folio} ha sido rechazada. " +
                    $"{(string.IsNullOrEmpty(motivo) ? "" : $"Motivo: {motivo}")}"
                );
            }

            TempData["ErrorMessage"] = $"Preinscripción {entidad.academiccontrol_preinscription_folio} rechazada.";
            return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
        }

        [HttpPost]
        public async Task<IActionResult> ValidarActa(int id)
        {
            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_actaValidada = true;
            await _context.SaveChangesAsync();

            var preinscripcion = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == entidad.academiccontrol_inscription_preinscriptionID);

            if (preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Documento Validado — Acta de Nacimiento",
                    $"Estimado(a) {preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"su Acta de Nacimiento ha sido validada correctamente. En breve se revisarán los demás documentos."
                );
            }

            TempData["SuccessMessage"] = "Acta de nacimiento validada.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        [HttpPost]
        public async Task<IActionResult> ValidarCurp(int id)
        {
            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_curpValidado = true;
            await _context.SaveChangesAsync();

            var preinscripcion = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == entidad.academiccontrol_inscription_preinscriptionID);

            if (preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Documento Validado — CURP",
                    $"Estimado(a) {preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"su CURP ha sido validado correctamente. En breve se revisarán los demás documentos."
                );
            }

            TempData["SuccessMessage"] = "CURP validado.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        // VALIDAR BOLETA
        [HttpPost]
        public async Task<IActionResult> ValidarBoleta(int id)
        {
            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_boletaValidada = true;

            // Si los 3 documentos están validados, cambiar estado automáticamente
            if (entidad.academiccontrol_inscription_actaValidada &&
                entidad.academiccontrol_inscription_curpValidado &&
                entidad.academiccontrol_inscription_boletaValidada)
            {
                entidad.academiccontrol_inscription_state = EstadoInscripcion.DocumentosValidados.ToString();

                var preinscripcion = await _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == entidad.academiccontrol_inscription_preinscriptionID);

                if (preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
                {
                    await _emailService.EnviarAsync(
                        preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                        "Documentos Validados",
                        $"Estimado(a) {preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                        $"todos sus documentos han sido validados correctamente."
                    );
                }

                TempData["SuccessMessage"] = "Boleta validada. Todos los documentos han sido validados.";
            }
            else
            {
                TempData["SuccessMessage"] = "Boleta validada.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarInscripcion(int id)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            // Validar que los documentos estén validados o el pago validado antes de aprobar
            if (entidad.academiccontrol_inscription_state != EstadoInscripcion.DocumentosValidados.ToString()
                && entidad.academiccontrol_inscription_state != EstadoInscripcion.PagoValidado.ToString())
            {
                TempData["ErrorMessage"] = "Los documentos o el pago deben estar validados antes de aprobar la inscripción.";
                return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
            }

            entidad.academiccontrol_inscription_state = EstadoInscripcion.Aprobada.ToString();

            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Documento Validado — Boleta de Estudios",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"su Boleta de Estudios ha sido validada correctamente."
                );

                // Si los 3 están validados, mandar correo de documentos completos
                if (entidad.academiccontrol_inscription_actaValidada &&
                    entidad.academiccontrol_inscription_curpValidado &&
                    entidad.academiccontrol_inscription_boletaValidada)
                {
                    await _emailService.EnviarAsync(
                        entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                        "Documentación Completa — Inscripción en Proceso",
                        $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                        $"todos sus documentos han sido validados correctamente. Su inscripción está siendo procesada."
                    );
                }
            }

            TempData["SuccessMessage"] = $"Inscripción aprobada. Matrícula: {entidad.academiccontrol_inscription_enrollment}.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        [HttpPost]
        public async Task<IActionResult> RechazarInscripcion(int id, string? motivo)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_state = EstadoInscripcion.Rechazada.ToString();
            entidad.academiccontrol_inscription_rejectionReason = motivo;

            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Inscripción Rechazada",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"lamentablemente su inscripción ha sido rechazada. " +
                    $"{(string.IsNullOrEmpty(motivo) ? "" : $"Motivo: {motivo}")}"
                );
            }

            TempData["ErrorMessage"] = "Inscripción rechazada.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        public async Task<IActionResult> ConfigurarFichas()
        {
            var configuraciones = await _context.ConfiguracionFichas
                .OrderBy(c => c.academiccontrol_inscription_ticketconfig_career)
                .ToListAsync();

            var periodos = await _context.PeriodosInscripcion
                .OrderByDescending(p => p.academiccontrol_inscription_period_startDate)
                .ToListAsync();

            var vm = new ConfiguracionViewModel
            {
                Configuraciones = configuraciones,
                Periodos = periodos
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPeriodo(PeriodoInscripcionEntity periodo)
        {
            if (periodo.academiccontrol_inscription_period_ID == 0)
            {
                periodo.academiccontrol_inscription_period_createdDate = DateTime.Now;
                _context.PeriodosInscripcion.Add(periodo);
            }
            else
            {
                var existing = await _context.PeriodosInscripcion.FindAsync(periodo.academiccontrol_inscription_period_ID);
                if (existing == null) return NotFound();

                existing.academiccontrol_inscription_period_name = periodo.academiccontrol_inscription_period_name;
                existing.academiccontrol_inscription_period_startDate = periodo.academiccontrol_inscription_period_startDate;
                existing.academiccontrol_inscription_period_endDate = periodo.academiccontrol_inscription_period_endDate;
                existing.academiccontrol_inscription_period_status = periodo.academiccontrol_inscription_period_status;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Periodo guardado correctamente.";
            return RedirectToAction(nameof(ConfigurarFichas));
        }

        [HttpPost]
        public async Task<IActionResult> GuardarConfiguracionFichas(ConfiguracionFichasEntity config)
        {
            // Validar que no exista otra configuración para la misma carrera
            if (config.academiccontrol_inscription_ticketconfig_ID == 0)
            {
                var existe = await _context.ConfiguracionFichas
                    .AnyAsync(c => c.academiccontrol_inscription_ticketconfig_career ==
                                   config.academiccontrol_inscription_ticketconfig_career);

                if (existe)
                {
                    TempData["ErrorMessage"] = $"Ya existe una configuración para la carrera '{config.academiccontrol_inscription_ticketconfig_career}'.";
                    return RedirectToAction(nameof(ConfigurarFichas));
                }
            }

            if (config.academiccontrol_inscription_ticketconfig_ID == 0)
            {
                config.academiccontrol_inscription_ticketconfig_createdDate = DateTime.Now;
                config.academiccontrol_inscription_ticketconfig_updatedDate = DateTime.Now;
                _context.ConfiguracionFichas.Add(config);
            }
            else
            {
                var existing = await _context.ConfiguracionFichas
                    .FindAsync(config.academiccontrol_inscription_ticketconfig_ID);

                if (existing == null) return NotFound();

                existing.academiccontrol_inscription_ticketconfig_career = config.academiccontrol_inscription_ticketconfig_career;
                existing.academiccontrol_inscription_ticketconfig_limit = config.academiccontrol_inscription_ticketconfig_limit;
                existing.academiccontrol_inscription_ticketconfig_inscriptionLimit = config.academiccontrol_inscription_ticketconfig_inscriptionLimit;
                existing.academiccontrol_inscription_ticketconfig_startDate = config.academiccontrol_inscription_ticketconfig_startDate;
                existing.academiccontrol_inscription_ticketconfig_endDate = config.academiccontrol_inscription_ticketconfig_endDate;
                existing.academiccontrol_inscription_ticketconfig_status = config.academiccontrol_inscription_ticketconfig_status;
                existing.academiccontrol_inscription_ticketconfig_updatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración guardada correctamente.";
            return RedirectToAction(nameof(ConfigurarFichas));
        }

        public async Task<IActionResult> ExportarHistorialExcel(
    string? carrera = null,
    string? estado = null,
    string? municipio = null,
    int? anio = null,
    string? fechaInicio = null,
    string? fechaFin = null,
    string tab = "preinscripciones")
        {
            carrera = string.IsNullOrEmpty(carrera) ? null : carrera;
            estado = string.IsNullOrEmpty(estado) ? null : estado;
            municipio = string.IsNullOrEmpty(municipio) ? null : municipio;

            DateTime? fechaInicioDate = string.IsNullOrEmpty(fechaInicio) ? null : DateTime.Parse(fechaInicio);
            DateTime? fechaFinDate = string.IsNullOrEmpty(fechaFin) ? null : DateTime.Parse(fechaFin);

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            if (tab == "inscripciones")
            {
                var query = _context.Inscripciones
                    .Include(i => i.Preinscripcion)
                        .ThenInclude(p => p.DatosPersonales)
                    .Include(i => i.Preinscripcion)
                        .ThenInclude(p => p.Domicilio)
                    .AsQueryable();

                if (carrera != null)
                    query = query.Where(i => i.academiccontrol_inscription_careerRequested == carrera);
                if (estado != null)
                    query = query.Where(i => i.academiccontrol_inscription_state == estado);
                if (municipio != null)
                    query = query.Where(i => i.Preinscripcion.Domicilio!.academiccontrol_preinscription_address_municipality == municipio);
                if (anio.HasValue)
                    query = query.Where(i => i.academiccontrol_inscription_registrationDate.Year == anio.Value);
                if (fechaInicioDate.HasValue)
                    query = query.Where(i => i.academiccontrol_inscription_registrationDate >= fechaInicioDate.Value);
                if (fechaFinDate.HasValue)
                    query = query.Where(i => i.academiccontrol_inscription_registrationDate <= fechaFinDate.Value);

                var datos = await query.OrderByDescending(i => i.academiccontrol_inscription_registrationDate).ToListAsync();
                var ws = workbook.Worksheets.Add("Inscripciones");

                var headers = new[]
                {
            "Matrícula", "Folio", "Apellido Paterno", "Apellido Materno", "Nombre",
            "CURP", "Email", "Carrera", "Municipio", "Estado Registro", "Fecha Registro"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#13322B");
                    ws.Cell(1, i + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                }

                int row = 2;
                foreach (var item in datos)
                {
                    ws.Cell(row, 1).Value = item.academiccontrol_inscription_enrollment ?? $"#{item.academiccontrol_inscription_ID}";
                    ws.Cell(row, 2).Value = item.Preinscripcion?.academiccontrol_preinscription_folio ?? "";
                    ws.Cell(row, 3).Value = item.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname ?? "";
                    ws.Cell(row, 4).Value = item.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_maternalSurname ?? "";
                    ws.Cell(row, 5).Value = item.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_name ?? "";
                    ws.Cell(row, 6).Value = item.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP ?? "";
                    ws.Cell(row, 7).Value = item.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email ?? "";
                    ws.Cell(row, 8).Value = item.academiccontrol_inscription_careerRequested;
                    ws.Cell(row, 9).Value = item.Preinscripcion?.Domicilio?.academiccontrol_preinscription_address_municipality ?? "";
                    ws.Cell(row, 10).Value = item.academiccontrol_inscription_state;
                    ws.Cell(row, 11).Value = item.academiccontrol_inscription_registrationDate.ToString("dd/MM/yyyy HH:mm");
                    row++;
                }

                ws.Columns().AdjustToContents();
            }
            else
            {
                // lógica de preinscripciones que ya tenías, igual que antes
                var query = _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .Include(p => p.Domicilio)
                    .Include(p => p.DatosEscolares)
                    .AsQueryable();

                if (carrera != null)
                    query = query.Where(p => p.academiccontrol_preinscription_careerRequested == carrera);
                if (estado != null)
                    query = query.Where(p => p.academiccontrol_preinscription_state == estado);
                if (municipio != null)
                    query = query.Where(p => p.Domicilio!.academiccontrol_preinscription_address_municipality == municipio);
                if (anio.HasValue)
                    query = query.Where(p => p.academiccontrol_preinscription_registrationDate.Year == anio.Value);
                if (fechaInicioDate.HasValue)
                    query = query.Where(p => p.academiccontrol_preinscription_registrationDate >= fechaInicioDate.Value);
                if (fechaFinDate.HasValue)
                    query = query.Where(p => p.academiccontrol_preinscription_registrationDate <= fechaFinDate.Value);

                var datos = await query.OrderByDescending(p => p.academiccontrol_preinscription_registrationDate).ToListAsync();
                var ws = workbook.Worksheets.Add("Preinscripciones");

                var headers = new[]
                {
            "Folio", "Apellido Paterno", "Apellido Materno", "Nombre",
            "CURP", "Fecha Nacimiento", "Género", "Email", "Teléfono",
            "Carrera", "Promedio", "Estado", "Municipio",
            "Escuela Procedencia", "Estado Registro", "Fecha Registro"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#13322B");
                    ws.Cell(1, i + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                }

                int row = 2;
                foreach (var item in datos)
                {
                    ws.Cell(row, 1).Value = item.academiccontrol_preinscription_folio ?? $"#{item.academiccontrol_preinscription_ID}";
                    ws.Cell(row, 2).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname ?? "";
                    ws.Cell(row, 3).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_maternalSurname ?? "";
                    ws.Cell(row, 4).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_name ?? "";
                    ws.Cell(row, 5).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP ?? "";
                    ws.Cell(row, 6).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_birthDate.ToString("dd/MM/yyyy") ?? "";
                    ws.Cell(row, 7).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_gender ?? "";
                    ws.Cell(row, 8).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_email ?? "";
                    ws.Cell(row, 9).Value = item.DatosPersonales?.academiccontrol_preinscription_personaldata_phone ?? "";
                    ws.Cell(row, 10).Value = item.academiccontrol_preinscription_careerRequested;
                    ws.Cell(row, 11).Value = item.academiccontrol_preinscription_average;
                    ws.Cell(row, 12).Value = item.Domicilio?.academiccontrol_preinscription_address_state ?? "";
                    ws.Cell(row, 13).Value = item.Domicilio?.academiccontrol_preinscription_address_municipality ?? "";
                    ws.Cell(row, 14).Value = item.DatosEscolares?.academiccontrol_preinscription_academic_originSchool ?? "";
                    ws.Cell(row, 15).Value = item.academiccontrol_preinscription_state;
                    ws.Cell(row, 16).Value = item.academiccontrol_preinscription_registrationDate.ToString("dd/MM/yyyy HH:mm");
                    row++;
                }

                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Historial_{(tab == "inscripciones" ? "Inscripciones" : "Preinscripciones")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> Historial(
    string? carrera = null,
    string? estado = null,
    string? municipio = null,
    int? anio = null,
    DateTime? fechaInicio = null,
    DateTime? fechaFin = null,
    string tab = "preinscripciones")
        {
            carrera = string.IsNullOrEmpty(carrera) ? null : carrera;
            estado = string.IsNullOrEmpty(estado) ? null : estado;
            municipio = string.IsNullOrEmpty(municipio) ? null : municipio;

            var queryPre = _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.DatosEscolares)
                .AsQueryable();

            var queryIns = _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .AsQueryable();

            if (carrera != null)
            {
                queryPre = queryPre.Where(p => p.academiccontrol_preinscription_careerRequested == carrera);
                queryIns = queryIns.Where(i => i.academiccontrol_inscription_careerRequested == carrera);
            }
            if (estado != null)
            {
                queryPre = queryPre.Where(p => p.academiccontrol_preinscription_state == estado);
                queryIns = queryIns.Where(i => i.academiccontrol_inscription_state == estado);
            }
            if (municipio != null)
            {
                queryPre = queryPre.Where(p => p.Domicilio!.academiccontrol_preinscription_address_municipality == municipio);
                queryIns = queryIns.Where(i => i.Preinscripcion.Domicilio!.academiccontrol_preinscription_address_municipality == municipio);
            }
            if (anio.HasValue)
            {
                queryPre = queryPre.Where(p => p.academiccontrol_preinscription_registrationDate.Year == anio.Value);
                queryIns = queryIns.Where(i => i.academiccontrol_inscription_registrationDate.Year == anio.Value);
            }
            if (fechaInicio.HasValue)
            {
                queryPre = queryPre.Where(p => p.academiccontrol_preinscription_registrationDate >= fechaInicio.Value);
                queryIns = queryIns.Where(i => i.academiccontrol_inscription_registrationDate >= fechaInicio.Value);
            }
            if (fechaFin.HasValue)
            {
                queryPre = queryPre.Where(p => p.academiccontrol_preinscription_registrationDate <= fechaFin.Value);
                queryIns = queryIns.Where(i => i.academiccontrol_inscription_registrationDate <= fechaFin.Value);
            }

            var vm = new HistorialViewModel
            {
                Preinscripciones = await queryPre.OrderByDescending(p => p.academiccontrol_preinscription_registrationDate).ToListAsync(),
                Inscripciones = await queryIns.OrderByDescending(i => i.academiccontrol_inscription_registrationDate).ToListAsync(),
                FiltroCarrera = carrera,
                FiltroEstado = estado,
                FiltroMunicipio = municipio,
                FiltroAnio = anio,
                FiltroFechaInicio = fechaInicio,
                FiltroFechaFin = fechaFin,
                TabActiva = tab,
                AniosDisponibles = await _context.Preinscripciones
                    .Select(p => p.academiccontrol_preinscription_registrationDate.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}