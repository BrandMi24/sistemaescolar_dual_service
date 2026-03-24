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

        // INDEX 
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

            // Filtros preinscripciones
            if (!string.IsNullOrEmpty(carrera))
                preinscripciones = preinscripciones
                    .Where(p => p.CarreraSolicitada == carrera).ToList();

            if (!string.IsNullOrEmpty(estado))
                preinscripciones = preinscripciones
                    .Where(p => p.EstadoPreinscripcion == estado).ToList();

            // Filtros inscripciones
            if (!string.IsNullOrEmpty(carrera))
                inscripciones = inscripciones
                    .Where(i => i.CarreraSolicitada == carrera).ToList();

            if (!string.IsNullOrEmpty(estado))
                inscripciones = inscripciones
                    .Where(i => i.EstadoInscripcion == estado).ToList();

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

        // VALIDAR PREINSCRIPCION 
        [HttpPost]
        public async Task<IActionResult> ValidarPreinscripcion(int id)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            entidad.EstadoPreinscripcion = EstadoPreinscripcion.Validada.ToString();
            await _context.SaveChangesAsync();

            // Notificar al aspirante
            if (entidad.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.Email,
                    "Preinscripción Validada",
                    $"Estimado(a) {entidad.DatosPersonales.Nombre}, " +
                    $"su preinscripción con folio {entidad.Folio} ha sido validada correctamente."
                );
            }

            TempData["SuccessMessage"] = $"Preinscripción {entidad.Folio} validada correctamente.";
            return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
        }

        // HABILITAR INSCRIPCION 
        [HttpPost]
        public async Task<IActionResult> HabilitarInscripcion(int id)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            if (entidad.EstadoPreinscripcion != EstadoPreinscripcion.Validada.ToString())
            {
                TempData["ErrorMessage"] = "La preinscripción debe estar validada antes de habilitar la inscripción.";
                return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
            }

            entidad.EstadoPreinscripcion = EstadoPreinscripcion.InscripcionHabilitada.ToString();
            await _context.SaveChangesAsync();

            // Notificar al aspirante
            if (entidad.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.Email,
                    "Inscripción Habilitada",
                    $"Estimado(a) {entidad.DatosPersonales.Nombre}, " +
                    $"su inscripción ha sido habilitada. Ya puede proceder con el proceso de inscripción " +
                    $"usando su folio: {entidad.Folio}."
                );
            }

            TempData["SuccessMessage"] = $"Inscripción habilitada para el folio {entidad.Folio}.";
            return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
        }

        // RECHAZAR PREINSCRIPCION
        [HttpPost]
        public async Task<IActionResult> RechazarPreinscripcion(int id, string? motivo)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            entidad.EstadoPreinscripcion = EstadoPreinscripcion.Rechazada.ToString();
            await _context.SaveChangesAsync();

            if (entidad.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.DatosPersonales.Email,
                    "Preinscripción Rechazada",
                    $"Estimado(a) {entidad.DatosPersonales.Nombre}, " +
                    $"lamentablemente su preinscripción con folio {entidad.Folio} ha sido rechazada. " +
                    $"{(string.IsNullOrEmpty(motivo) ? "" : $"Motivo: {motivo}")}"
                );
            }

            TempData["ErrorMessage"] = $"Preinscripción {entidad.Folio} rechazada.";
            return RedirectToAction(nameof(Index), new { tab = "preinscripciones" });
        }

        // VALIDAR DOCUMENTOS INSCRIPCION 
        [HttpPost]
        public async Task<IActionResult> ValidarDocumentos(int id)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            entidad.EstadoInscripcion = EstadoInscripcion.DocumentosValidados.ToString();
            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.Email,
                    "Documentos Validados",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.Nombre}, " +
                    $"sus documentos han sido validados correctamente."
                );
            }

            TempData["SuccessMessage"] = "Documentos validados correctamente.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        // APROBAR INSCRIPCION 
        [HttpPost]
        public async Task<IActionResult> AprobarInscripcion(int id)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            if (entidad.EstadoInscripcion != EstadoInscripcion.PagoValidado.ToString())
            {
                TempData["ErrorMessage"] = "El pago debe estar validado antes de aprobar la inscripción.";
                return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
            }

            entidad.EstadoInscripcion = EstadoInscripcion.Aprobada.ToString();
            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.Email,
                    "Inscripción Aprobada",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.Nombre}, " +
                    $"su inscripción ha sido aprobada. Su matrícula es: {entidad.Matricula}."
                );
            }

            TempData["SuccessMessage"] = $"Inscripción aprobada. Matrícula: {entidad.Matricula}.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        // RECHAZAR INSCRIPCION 
        [HttpPost]
        public async Task<IActionResult> RechazarInscripcion(int id, string? motivo)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            entidad.EstadoInscripcion = EstadoInscripcion.Rechazada.ToString();
            await _context.SaveChangesAsync();

            if (entidad.Preinscripcion?.DatosPersonales?.Email != null)
            {
                await _emailService.EnviarAsync(
                    entidad.Preinscripcion.DatosPersonales.Email,
                    "Inscripción Rechazada",
                    $"Estimado(a) {entidad.Preinscripcion.DatosPersonales.Nombre}, " +
                    $"lamentablemente su inscripción ha sido rechazada. " +
                    $"{(string.IsNullOrEmpty(motivo) ? "" : $"Motivo: {motivo}")}"
                );
            }

            TempData["ErrorMessage"] = "Inscripción rechazada.";
            return RedirectToAction(nameof(Index), new { tab = "inscripciones" });
        }

        // CONFIGURACION FICHAS 
        public async Task<IActionResult> ConfigurarFichas()
        {
            var configuraciones = await _context.ConfiguracionFichas
                .OrderBy(c => c.Carrera)
                .ToListAsync();

            return View(configuraciones);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarConfiguracionFichas(ConfiguracionFichasEntity config)
        {
            if (config.Id == 0)
            {
                config.FechaCreacion = DateTime.Now;
                config.FechaActualizacion = DateTime.Now;
                _context.ConfiguracionFichas.Add(config);
            }
            else
            {
                var existing = await _context.ConfiguracionFichas.FindAsync(config.Id);
                if (existing == null) return NotFound();

                existing.Carrera = config.Carrera;
                existing.LimiteFichas = config.LimiteFichas;
                existing.FechaInicio = config.FechaInicio;
                existing.FechaFin = config.FechaFin;
                existing.Activo = config.Activo;
                existing.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración guardada correctamente.";
            return RedirectToAction(nameof(ConfigurarFichas));
        }
    }
}