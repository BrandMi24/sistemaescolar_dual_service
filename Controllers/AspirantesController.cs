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

        //[Authorize]
        //public async Task<IActionResult> Details(int? id, string? returnUrl = null)
        //{
        //    if (id == null) return NotFound();

        //    var entidad = await _context.Inscripciones
        //        .Include(i => i.Preinscripcion)
        //            .ThenInclude(p => p.DatosPersonales)
        //        .Include(i => i.Preinscripcion)
        //            .ThenInclude(p => p.Domicilio)
        //        .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

        //    if (entidad == null) return NotFound();

        //    ViewBag.ReturnUrl = returnUrl;
        //    return View(MapToViewModel(entidad));
        //}

        public async Task<IActionResult> Details(int id)
        {
            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosEscolares)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Tutor)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == id);

            if (entidad == null) return NotFound();

            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("ControlEscolar") || referer.Contains("Historial"))
                ViewBag.ReturnUrl = referer;
            else
                ViewBag.ReturnUrl = Url.Action("Index", "Home");

            return View(MapToViewModel(entidad));
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aspirante vm)
        {

            if (vm.ActaNacimientoFile == null || vm.ActaNacimientoFile.Length == 0)
                return Json(new { success = false, message = "El Acta de Nacimiento es requerida." });

            if (vm.CurpPdfFile == null || vm.CurpPdfFile.Length == 0)
                return Json(new { success = false, message = "El CURP es requerido." });

            if (vm.BoletaPdfFile == null || vm.BoletaPdfFile.Length == 0)
                return Json(new { success = false, message = "El Certificado de Estudios o Boleta es requerido." });

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
                    return Json(new { success = false, message = "Aspirante ya inscrito" });
                }

                var configInscripcion = await _context.ConfiguracionFichas
    .FirstOrDefaultAsync(c => c.academiccontrol_inscription_ticketconfig_career == preinscripcion.academiccontrol_preinscription_careerRequested
                           && c.academiccontrol_inscription_ticketconfig_status
                           && c.academiccontrol_inscription_ticketconfig_startDate <= DateTime.Today
                           && c.academiccontrol_inscription_ticketconfig_endDate >= DateTime.Today);

                if (configInscripcion == null)
                    return Json(new { success = false, message = $"El periodo de inscripción para '{preinscripcion.academiccontrol_preinscription_careerRequested}' no está activo en este momento." });

                if (configInscripcion.academiccontrol_inscription_ticketconfig_inscriptionLimit.HasValue)
                {
                    var inscripcionesUsadas = await _context.Inscripciones
                        .CountAsync(i => i.academiccontrol_inscription_careerRequested == preinscripcion.academiccontrol_preinscription_careerRequested
                                      && i.academiccontrol_inscription_registrationDate >= configInscripcion.academiccontrol_inscription_ticketconfig_startDate
                                      && i.academiccontrol_inscription_registrationDate <= configInscripcion.academiccontrol_inscription_ticketconfig_endDate);

                    if (inscripcionesUsadas >= configInscripcion.academiccontrol_inscription_ticketconfig_inscriptionLimit.Value)
                        return Json(new { success = false, message = $"Cupo completo para inscripción en '{preinscripcion.academiccontrol_preinscription_careerRequested}'." });
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
                    academiccontrol_inscription_careerRequested = !string.IsNullOrWhiteSpace(vm.academiccontrol_inscription_careerRequested)
                        ? vm.academiccontrol_inscription_careerRequested
                        : preinscripcion.academiccontrol_preinscription_careerRequested,
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

        // GET: Aspirantes/AccesoCorreccion — Formulario de acceso (sin datos en URL)
        public IActionResult AccesoCorreccion()
        {
            return View();
        }

        // POST: Aspirantes/AccesoCorreccion — Valida folio+CURP y redirige internamente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccesoCorreccion(string folio, string curp)
        {
            if (string.IsNullOrWhiteSpace(folio) || string.IsNullOrWhiteSpace(curp))
            {
                TempData["AccesoError"] = "Debes ingresar tu Folio y tu CURP.";
                return RedirectToAction(nameof(AccesoCorreccion));
            }

            var preinscripcion = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_folio == folio.Trim()
                    && p.DatosPersonales!.academiccontrol_preinscription_personaldata_CURP == curp.Trim().ToUpper());

            if (preinscripcion == null)
            {
                TempData["AccesoError"] = "No encontramos una preinscripción con los datos ingresados. Verifica tu folio y CURP.";
                return RedirectToAction(nameof(AccesoCorreccion));
            }

            var inscripcion = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_preinscriptionID == preinscripcion.academiccontrol_preinscription_ID
                    && i.academiccontrol_inscription_state == EstadoInscripcion.DocumentoConError.ToString());

            if (inscripcion == null)
            {
                TempData["AccesoError"] = "No tienes documentos pendientes de corrección en este momento.";
                return RedirectToAction(nameof(AccesoCorreccion));
            }

            // Guardamos en TempData — nunca va en la URL
            TempData["CorrFolio"] = folio.Trim();
            TempData["CorrCurp"] = curp.Trim().ToUpper();
            return RedirectToAction(nameof(CorregirDocumentos));
        }

        // GET: Aspirantes/CorregirDocumentos — Lee de TempData, sin parámetros en la URL
        public async Task<IActionResult> CorregirDocumentos()
        {
            var folio = TempData["CorrFolio"] as string;
            var curp = TempData["CorrCurp"] as string;

            if (string.IsNullOrWhiteSpace(folio) || string.IsNullOrWhiteSpace(curp))
                return RedirectToAction(nameof(AccesoCorreccion));

            var preinscripcion = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_folio == folio
                    && p.DatosPersonales!.academiccontrol_preinscription_personaldata_CURP == curp);

            if (preinscripcion == null)
                return View("CorregirDocumentosError", "No se encontró una preinscripción con los datos ingresados.");

            var inscripcion = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_preinscriptionID == preinscripcion.academiccontrol_preinscription_ID
                    && i.academiccontrol_inscription_state == EstadoInscripcion.DocumentoConError.ToString());

            if (inscripcion == null)
                return View("CorregirDocumentosError", "No hay documentos pendientes de corrección para su inscripción.");

            var vm = new CorreccionDocumentosViewModel
            {
                InscripcionId = inscripcion.academiccontrol_inscription_ID,
                Folio = folio,
                Curp = curp,
                NombreCompleto = $"{preinscripcion.DatosPersonales?.academiccontrol_preinscription_personaldata_name} " +
                                 $"{preinscripcion.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname} " +
                                 $"{preinscripcion.DatosPersonales?.academiccontrol_preinscription_personaldata_maternalSurname}",
                Matricula = inscripcion.academiccontrol_inscription_enrollment ?? $"#{inscripcion.academiccontrol_inscription_ID}",
                ActaConError = inscripcion.academiccontrol_inscription_actaConError,
                CurpConError = inscripcion.academiccontrol_inscription_curpConError,
                BoletaConError = inscripcion.academiccontrol_inscription_boletaConError,
                MotivoError = inscripcion.academiccontrol_inscription_errorReason
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorregirDocumentos(CorreccionDocumentosViewModel vm)
        {
            var inscripcion = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.academiccontrol_inscription_ID == vm.InscripcionId
                    && i.academiccontrol_inscription_state == EstadoInscripcion.DocumentoConError.ToString());

            if (inscripcion == null)
                return View("CorregirDocumentosError", "Inscripción no encontrada o ya procesada.");

            // Validar que el CURP coincide (seguridad)
            if (inscripcion.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP != vm.Curp)
                return View("CorregirDocumentosError", "Los datos no coinciden.");

            bool algoCambiado = false;

            // Acta
            if (inscripcion.academiccontrol_inscription_actaConError && vm.ActaNacimientoFile != null && vm.ActaNacimientoFile.Length > 0)
            {
                _fileService.DeleteFile(inscripcion.academiccontrol_inscription_birthCertificatePath);
                inscripcion.academiccontrol_inscription_birthCertificatePath = await _fileService.SavePdfAsync(vm.ActaNacimientoFile, "inscripciones");
                inscripcion.academiccontrol_inscription_actaConError = false;
                algoCambiado = true;
            }

            // CURP
            if (inscripcion.academiccontrol_inscription_curpConError && vm.CurpPdfFile != null && vm.CurpPdfFile.Length > 0)
            {
                _fileService.DeleteFile(inscripcion.academiccontrol_inscription_curpPdfPath);
                inscripcion.academiccontrol_inscription_curpPdfPath = await _fileService.SavePdfAsync(vm.CurpPdfFile, "inscripciones");
                inscripcion.academiccontrol_inscription_curpConError = false;
                algoCambiado = true;
            }

            // Boleta
            if (inscripcion.academiccontrol_inscription_boletaConError && vm.BoletaPdfFile != null && vm.BoletaPdfFile.Length > 0)
            {
                _fileService.DeleteFile(inscripcion.academiccontrol_inscription_transcriptPath);
                inscripcion.academiccontrol_inscription_transcriptPath = await _fileService.SavePdfAsync(vm.BoletaPdfFile, "inscripciones");
                inscripcion.academiccontrol_inscription_boletaConError = false;
                algoCambiado = true;
            }

            if (!algoCambiado)
            {
                ModelState.AddModelError("", "Debe subir al menos un documento PDF para corregir.");
                return View(vm);
            }

            // Si no quedan documentos con error, regresar a Pendiente para revisión del admin
            bool quedanErrores = inscripcion.academiccontrol_inscription_actaConError
                              || inscripcion.academiccontrol_inscription_curpConError
                              || inscripcion.academiccontrol_inscription_boletaConError;

            if (!quedanErrores)
                inscripcion.academiccontrol_inscription_state = EstadoInscripcion.Pendiente.ToString();

            inscripcion.academiccontrol_inscription_errorReason = null;
            await _context.SaveChangesAsync();

            TempData["CorreccionCompletadaId"] = inscripcion.academiccontrol_inscription_ID;
            TempData["CorreccionCompletadaNombre"] = vm.NombreCompleto;
            TempData["CorreccionCompletadaMatricula"] = vm.Matricula;
            return RedirectToAction(nameof(CorreccionCompletada));
        }

        public IActionResult CorreccionCompletada()
        {
            if (TempData["CorreccionCompletadaId"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.InscripcionId = (int)TempData["CorreccionCompletadaId"]!;
            ViewBag.Nombre = TempData["CorreccionCompletadaNombre"] as string ?? "";
            ViewBag.Matricula = TempData["CorreccionCompletadaMatricula"] as string ?? "";
            return View();
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
            academiccontrol_inscription_enrollment = e.academiccontrol_inscription_enrollment,
            academiccontrol_inscription_birthCertificatePath = e.academiccontrol_inscription_birthCertificatePath,
            academiccontrol_inscription_curpPdfPath = e.academiccontrol_inscription_curpPdfPath,
            academiccontrol_inscription_transcriptPath = e.academiccontrol_inscription_transcriptPath,
            academiccontrol_inscription_registrationDate = e.academiccontrol_inscription_registrationDate,
            academiccontrol_inscription_state = e.academiccontrol_inscription_state,
            MotivoRechazo = e.academiccontrol_inscription_rejectionReason,

            // Datos personales mapeados desde la preinscripción
            academiccontrol_preinscription_personaldata_name = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_name,
            academiccontrol_preinscription_personaldata_paternalSurname = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname,
            academiccontrol_preinscription_personaldata_CURP = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP,
            academiccontrol_preinscription_personaldata_email = e.Preinscripcion?.DatosPersonales?.academiccontrol_preinscription_personaldata_email
        };
    }
}