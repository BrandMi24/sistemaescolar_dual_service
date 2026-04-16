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
            
            //_context.AuditLogs.Add(new AuditLogEntity
            //{
            //    academiccontrol_audit_action = "Validar",
            //    academiccontrol_audit_entityName = "Preinscripcion",
            //    academiccontrol_audit_entityID = entidad.academiccontrol_preinscription_ID,
            //    academiccontrol_audit_user = User.Identity?.Name ?? "Sistema"
            //});

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
            
            //_context.AuditLogs.Add(new AuditLogEntity
            //{
            //    academiccontrol_audit_action = "Habilitar Inscripcion",
            //    academiccontrol_audit_entityName = "Preinscripcion",
            //    academiccontrol_audit_entityID = entidad.academiccontrol_preinscription_ID,
            //    academiccontrol_audit_user = User.Identity?.Name ?? "Sistema"
            //});

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
            
            //_context.AuditLogs.Add(new AuditLogEntity
            //{
            //    academiccontrol_audit_action = "Rechazar",
            //    academiccontrol_audit_entityName = "Preinscripcion",
            //    academiccontrol_audit_entityID = entidad.academiccontrol_preinscription_ID,
            //    academiccontrol_audit_user = User.Identity?.Name ?? "Sistema",
            //    academiccontrol_audit_details = motivo
            //});

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

        // VALIDAR ACTA DE NACIMIENTO
        [HttpPost]
        public async Task<IActionResult> ValidarActa(int id)
        {
            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_actaValidada = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Acta de nacimiento validada.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        // VALIDAR CURP
        [HttpPost]
        public async Task<IActionResult> ValidarCurp(int id)
        {
            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            entidad.academiccontrol_inscription_curpValidado = true;
            await _context.SaveChangesAsync();

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
            
            //_context.AuditLogs.Add(new AuditLogEntity
            //{
            //    academiccontrol_audit_action = "Aprobar",
            //    academiccontrol_audit_entityName = "Inscripcion",
            //    academiccontrol_audit_entityID = entidad.academiccontrol_inscription_ID,
            //    academiccontrol_audit_user = User.Identity?.Name ?? "Sistema"
            //});

            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_email,
                    "Inscripción Aprobada",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.academiccontrol_preinscription_personaldata_name}, " +
                    $"su inscripción ha sido aprobada. Su matrícula es: {entidad.academiccontrol_inscription_enrollment}."
                );
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
            
            //_context.AuditLogs.Add(new AuditLogEntity
            //{
            //    academiccontrol_audit_action = "Rechazar",
            //    academiccontrol_audit_entityName = "Inscripcion",
            //    academiccontrol_audit_entityID = entidad.academiccontrol_inscription_ID,
            //    academiccontrol_audit_user = User.Identity?.Name ?? "Sistema",
            //    academiccontrol_audit_details = motivo
            //});

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

        //[HttpPost]
        //public async Task<IActionResult> GuardarConfiguracionFichas(ConfiguracionFichasEntity config)
        //{
        //    if (config.academiccontrol_inscription_ticketconfig_ID == 0)
        //    {
        //        config.academiccontrol_inscription_ticketconfig_createdDate = DateTime.Now;
        //        config.academiccontrol_inscription_ticketconfig_updatedDate = DateTime.Now;
        //        _context.ConfiguracionFichas.Add(config);
        //    }
        //    else
        //    {
        //        var existing = await _context.ConfiguracionFichas.FindAsync(config.academiccontrol_inscription_ticketconfig_ID);
        //        if (existing == null) return NotFound();

        //        existing.academiccontrol_inscription_ticketconfig_career = config.academiccontrol_inscription_ticketconfig_career;
        //        existing.academiccontrol_inscription_ticketconfig_limit = config.academiccontrol_inscription_ticketconfig_limit;
        //        existing.academiccontrol_inscription_ticketconfig_startDate = config.academiccontrol_inscription_ticketconfig_startDate;
        //        existing.academiccontrol_inscription_ticketconfig_endDate = config.academiccontrol_inscription_ticketconfig_endDate;
        //        existing.academiccontrol_inscription_ticketconfig_status = config.academiccontrol_inscription_ticketconfig_status;
        //        existing.academiccontrol_inscription_ticketconfig_updatedDate = DateTime.Now;
        //    }

        //    await _context.SaveChangesAsync();
        //    TempData["SuccessMessage"] = "Configuración guardada correctamente.";
        //    return RedirectToAction(nameof(ConfigurarFichas));
        //}

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
                existing.academiccontrol_inscription_ticketconfig_startDate = config.academiccontrol_inscription_ticketconfig_startDate;
                existing.academiccontrol_inscription_ticketconfig_endDate = config.academiccontrol_inscription_ticketconfig_endDate;
                existing.academiccontrol_inscription_ticketconfig_status = config.academiccontrol_inscription_ticketconfig_status;
                existing.academiccontrol_inscription_ticketconfig_updatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración guardada correctamente.";
            return RedirectToAction(nameof(ConfigurarFichas));
        }

        public async Task<IActionResult> Historial(string? carrera = null)
        {
            var query = _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .AsQueryable();

            if (!string.IsNullOrEmpty(carrera))
                query = query.Where(p => p.academiccontrol_preinscription_careerRequested == carrera);

            var vm = new HistorialViewModel
            {
                Preinscripciones = await query.ToListAsync()
            };

            return View(vm);
        }
    }
}