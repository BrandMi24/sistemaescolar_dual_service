using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Data;
using ControlEscolar.Enums;
using ControlEscolar.Models;
using ControlEscolar.Services;

namespace ControlEscolar.Controllers
{
    public class AspirantesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IPdfService _pdfService;

        public AspirantesController(ApplicationDbContext context, IFileService fileService, IPdfService pdfService)
        {
            _context = context;
            _fileService = fileService;
            _pdfService = pdfService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var entidades = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .ToListAsync();

            return View(entidades.Select(e => MapToViewModel(e)).ToList());
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aspirante vm)
        {
            try
            {
                // 1. Validaciones de Negocio
                // Se busca por el folio de validación que viene del ViewModel
                var preinscripcion = await _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_folio == vm.FolioValidacion);

                if (preinscripcion == null)
                {
                    return Json(new { success = false, message = "El folio ingresado no existe." });
                }

                var periodoActivo = await _context.PeriodosInscripcion
                    .AnyAsync(p => p.academiccontrol_inscription_period_status
                                && p.academiccontrol_inscription_period_startDate <= DateTime.Today
                                && p.academiccontrol_inscription_period_endDate >= DateTime.Today);

                if (!periodoActivo)
                {
                    return Json(new { success = false, message = "El periodo de inscripción no está activo." });
                }

                if (preinscripcion.academiccontrol_preinscription_state != EstadoPreinscripcion.InscripcionHabilitada.ToString())
                {
                    return Json(new { success = false, message = "Su inscripción aún no ha sido habilitada." });
                }

                if (preinscripcion.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP != vm.CurpValidacion)
                {
                    return Json(new { success = false, message = "El CURP no coincide con el folio ingresado." });
                }

                var inscripcionExistente = await _context.Inscripciones
                    .AnyAsync(i => i.academiccontrol_inscription_preinscriptionID == preinscripcion.academiccontrol_preinscription_ID);

                if (inscripcionExistente)
                {
                    return Json(new { success = false, message = "Este folio ya cuenta con una inscripción registrada." });
                }

                // 2. Procesamiento de Archivos (Usando IFileService)
                if (vm.ActaNacimientoFile != null)
                    vm.academiccontrol_inscription_birthCertificatePath = await _fileService.SavePdfAsync(vm.ActaNacimientoFile, "inscripciones");

                if (vm.CurpPdfFile != null)
                    vm.academiccontrol_inscription_curpPdfPath = await _fileService.SavePdfAsync(vm.CurpPdfFile, "inscripciones");

                if (vm.BoletaPdfFile != null)
                    vm.academiccontrol_inscription_transcriptPath = await _fileService.SavePdfAsync(vm.BoletaPdfFile, "inscripciones");

                // 3. Persistencia
                var entidad = new InscripcionEntity
                {
                    academiccontrol_inscription_preinscriptionID = preinscripcion.academiccontrol_preinscription_ID,
                    academiccontrol_inscription_careerRequested = vm.academiccontrol_inscription_careerRequested,
                    academiccontrol_inscription_hasTSUEnrollment = vm.academiccontrol_inscription_hasTSUEnrollment,
                    academiccontrol_inscription_TSUEnrollment = vm.academiccontrol_inscription_TSUEnrollment,
                    academiccontrol_inscription_birthCertificatePath = vm.academiccontrol_inscription_birthCertificatePath,
                    academiccontrol_inscription_curpPdfPath = vm.academiccontrol_inscription_curpPdfPath,
                    academiccontrol_inscription_transcriptPath = vm.academiccontrol_inscription_transcriptPath,
                    academiccontrol_inscription_registrationDate = DateTime.Now,
                    academiccontrol_inscription_state = EstadoRegistro.Pendiente.ToString()
                };

                _context.Inscripciones.Add(entidad);
                await _context.SaveChangesAsync();

                // Generar matrícula oficial
                entidad.academiccontrol_inscription_enrollment = $"ITC-{DateTime.Now.Year}-{entidad.academiccontrol_inscription_ID:D5}";
                await _context.SaveChangesAsync();

                // Cargar datos para el PDF
                await _context.Entry(entidad).Reference(i => i.Preinscripcion).LoadAsync();
                await _context.Entry(entidad.Preinscripcion!).Reference(p => p.DatosPersonales).LoadAsync();
                await _context.Entry(entidad.Preinscripcion!).Reference(p => p.Domicilio).LoadAsync();
                await _context.Entry(entidad.Preinscripcion!).Reference(p => p.Tutor).LoadAsync();
                await _context.Entry(entidad.Preinscripcion!).Reference(p => p.DatosEscolares).LoadAsync();

                var pdfBytes = _pdfService.GenerarFichaInscripcion(entidad);

                Response.Headers.Append("X-Matricula", entidad.academiccontrol_inscription_enrollment);
                Response.Headers.Append("X-Details-Url", Url.Action("Details", "Aspirantes", new { id = entidad.academiccontrol_inscription_ID }));
                Response.Headers.Append("Access-Control-Expose-Headers", "X-Matricula, X-Details-Url");

                return File(pdfBytes, "application/pdf", $"Ficha_{entidad.academiccontrol_inscription_enrollment}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error: " + ex.Message });
            }
        }

        public async Task<IActionResult> DescargarFicha(int id)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Tutor)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosEscolares)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            var pdfBytes = _pdfService.GenerarFichaInscripcion(entidad);
            return File(pdfBytes, "application/pdf", $"Ficha_{entidad.academiccontrol_inscription_enrollment}.pdf");
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Aspirante vm)
        {
            var entidad = await _context.Inscripciones.FindAsync(id);
            if (entidad == null) return NotFound();

            if (ModelState.IsValid)
            {
                entidad.academiccontrol_inscription_careerRequested = vm.academiccontrol_inscription_careerRequested;
                entidad.academiccontrol_inscription_hasTSUEnrollment = vm.academiccontrol_inscription_hasTSUEnrollment;
                entidad.academiccontrol_inscription_TSUEnrollment = vm.academiccontrol_inscription_TSUEnrollment;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entidad = await _context.Inscripciones.FindAsync(id);
            if (entidad != null)
            {
                _fileService.DeleteFile(entidad.academiccontrol_inscription_birthCertificatePath);
                _fileService.DeleteFile(entidad.academiccontrol_inscription_curpPdfPath);
                _fileService.DeleteFile(entidad.academiccontrol_inscription_transcriptPath);
                _context.Inscripciones.Remove(entidad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static Aspirante MapToViewModel(InscripcionEntity e) => new()
        {
            academiccontrol_inscription_ID = e.academiccontrol_inscription_ID,
            FolioValidacion = e.Preinscripcion?.academiccontrol_preinscription_folio ?? string.Empty,
            academiccontrol_inscription_careerRequested = e.academiccontrol_inscription_careerRequested,
            academiccontrol_inscription_hasTSUEnrollment = e.academiccontrol_inscription_hasTSUEnrollment,
            academiccontrol_inscription_TSUEnrollment = e.academiccontrol_inscription_TSUEnrollment,
            academiccontrol_inscription_enrollment = e.academiccontrol_inscription_enrollment,
            academiccontrol_inscription_birthCertificatePath = e.academiccontrol_inscription_birthCertificatePath,
            academiccontrol_inscription_curpPdfPath = e.academiccontrol_inscription_curpPdfPath,
            academiccontrol_inscription_transcriptPath = e.academiccontrol_inscription_transcriptPath,
            academiccontrol_inscription_registrationDate = e.academiccontrol_inscription_registrationDate,
            academiccontrol_inscription_state = e.academiccontrol_inscription_state,

            // Datos personales mapeados desde la preinscripción
            academiccontrol_preinscription_personaldata_name = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_name,
            academiccontrol_preinscription_personaldata_paternalSurname = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname,
            academiccontrol_preinscription_personaldata_CURP = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP,
            academiccontrol_preinscription_personaldata_email = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email
        };
    }
}