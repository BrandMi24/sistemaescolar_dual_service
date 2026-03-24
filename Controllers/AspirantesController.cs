//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ControlEscolar.Data;
//using ControlEscolar.Enums;
//using ControlEscolar.Models;

//namespace ControlEscolar.Controllers
//{
//    public class AspirantesController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public AspirantesController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // GET: Aspirantes
//        [Authorize]
//        public async Task<IActionResult> Index()
//        {
//            return View(await _context.Aspirantes.ToListAsync());
//        }

//        // GET: Aspirantes/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var aspirante = await _context.Aspirantes
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (aspirante == null)
//            {
//                return NotFound();
//            }

//            return View(aspirante);
//        }

//        // GET: Aspirantes/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Aspirantes/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Aspirante aspirante)
//        {
//            if (ModelState.IsValid)
//            {
//                aspirante.FechaRegistro = DateTime.Now;
//                aspirante.EstadoRegistro = EstadoRegistro.Pendiente;
//                _context.Add(aspirante);
//                await _context.SaveChangesAsync();
//                TempData["SuccessMessage"] = "Su inscripción ha sido registrada exitosamente. Su número de folio es: " + aspirante.Id;
//                return RedirectToAction(nameof(Details), new { id = aspirante.Id });
//            }
//            return View(aspirante);
//        }

//        // GET: Aspirantes/Edit/5
//        [Authorize]
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var aspirante = await _context.Aspirantes.FindAsync(id);
//            if (aspirante == null)
//            {
//                return NotFound();
//            }
//            return View(aspirante);
//        }

//        // POST: Aspirantes/Edit/5
//        [HttpPost]
//        [Authorize]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, Aspirante aspirante)
//        {
//            if (id != aspirante.Id)
//            {
//                return NotFound();
//            }

//            // Cargar campos protegidos desde el registro existente
//            var existing = await _context.Aspirantes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
//            if (existing == null)
//            {
//                return NotFound();
//            }
//            aspirante.FechaRegistro = existing.FechaRegistro;

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(aspirante);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!AspiranteExists(aspirante.Id))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            return View(aspirante);
//        }

//        // GET: Aspirantes/Delete/5
//        [Authorize]
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var aspirante = await _context.Aspirantes
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (aspirante == null)
//            {
//                return NotFound();
//            }

//            return View(aspirante);
//        }

//        // POST: Aspirantes/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [Authorize]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var aspirante = await _context.Aspirantes.FindAsync(id);
//            if (aspirante != null)
//            {
//                _context.Aspirantes.Remove(aspirante);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool AspiranteExists(int id)
//        {
//            return _context.Aspirantes.Any(e => e.Id == id);
//        }
//    }
//}

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

        // GET: Aspirantes
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var entidades = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .ToListAsync();

            return View(entidades.Select(e => MapToViewModel(e)).ToList());
        }

        // GET: Aspirantes/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // GET: Aspirantes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Aspirantes/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Aspirante vm)
        //{
        //    ModelState.Remove("CurpValidacion");
        //    ModelState.Remove("Nombre");
        //    ModelState.Remove("ApellidoPaterno");
        //    ModelState.Remove("CURP");
        //    ModelState.Remove("FechaNacimiento");
        //    ModelState.Remove("Email");

        //    // Validar que el folio exista
        //    var preinscripcion = await _context.Preinscripciones
        //        .Include(p => p.DatosPersonales)
        //        .FirstOrDefaultAsync(p => p.Folio == vm.Folio);

        //    if (preinscripcion == null)
        //    {
        //        ModelState.AddModelError("Folio", "El folio ingresado no existe.");
        //        return View(vm);
        //    }

        //    // Validar que el CURP coincida
        //    if (preinscripcion.DatosPersonales?.CURP != vm.CurpValidacion)
        //    {
        //        ModelState.AddModelError("CurpValidacion", "El CURP no coincide con el folio ingresado.");
        //        return View(vm);
        //    }

        //    // Validar que no haya una inscripción previa con este folio
        //    var inscripcionExistente = await _context.Inscripciones
        //        .FirstOrDefaultAsync(i => i.PreinscripcionId == preinscripcion.Id);

        //    if (inscripcionExistente != null)
        //    {
        //        ModelState.AddModelError("Folio", "Este folio ya fue utilizado para una inscripción.");
        //        return View(vm);
        //    }

        //    // Manejo de archivos
        //    if (vm.ActaNacimientoFile != null)
        //    {
        //        try { vm.ActaNacimientoPath = await _fileService.SavePdfAsync(vm.ActaNacimientoFile, "inscripciones"); }
        //        catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.ActaNacimientoFile), ex.Message); }
        //    }
        //    else
        //    {
        //        ModelState.AddModelError(nameof(vm.ActaNacimientoFile), "Adjunte el acta de nacimiento en formato PDF.");
        //    }

        //    if (vm.CurpPdfFile != null)
        //    {
        //        try { vm.CurpPdfPath = await _fileService.SavePdfAsync(vm.CurpPdfFile, "inscripciones"); }
        //        catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.CurpPdfFile), ex.Message); }
        //    }
        //    else
        //    {
        //        ModelState.AddModelError(nameof(vm.CurpPdfFile), "Adjunte el CURP en formato PDF.");
        //    }

        //    if (vm.BoletaPdfFile != null)
        //    {
        //        try { vm.BoletaPdfPath = await _fileService.SavePdfAsync(vm.BoletaPdfFile, "inscripciones"); }
        //        catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.BoletaPdfFile), ex.Message); }
        //    }
        //    else
        //    {
        //        ModelState.AddModelError(nameof(vm.BoletaPdfFile), "Adjunte la boleta en formato PDF.");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        var entidad = new InscripcionEntity
        //        {
        //            PreinscripcionId = preinscripcion.Id,
        //            CarreraSolicitada = vm.CarreraSolicitada,
        //            TieneMatriculaTSU = vm.TieneMatriculaTSU,
        //            MatriculaTSU = vm.MatriculaTSU,
        //            ActaNacimientoPath = vm.ActaNacimientoPath,
        //            CurpPdfPath = vm.CurpPdfPath,
        //            BoletaPdfPath = vm.BoletaPdfPath,
        //            FechaInscripcion = DateTime.Now,
        //            EstadoInscripcion = EstadoRegistro.Pendiente.ToString()
        //        };

        //        _context.Inscripciones.Add(entidad);
        //        await _context.SaveChangesAsync();

        //        // Generar matrícula con el Id
        //        entidad.Matricula = $"ITC-{DateTime.Now.Year}-{entidad.Id:D5}";
        //        await _context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = $"Inscripción registrada exitosamente. Su matrícula es: {entidad.Matricula}";
        //        return RedirectToAction(nameof(Details), new { id = entidad.Id });
        //    }

        //    return View(vm);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aspirante vm)
        {
            ModelState.Remove("CurpValidacion");
            ModelState.Remove("Nombre");
            ModelState.Remove("ApellidoPaterno");
            ModelState.Remove("CURP");
            ModelState.Remove("FechaNacimiento");
            ModelState.Remove("Email");
            ModelState.Remove("Sexo");
            ModelState.Remove("Calle");
            ModelState.Remove("Estado");
            ModelState.Remove("Colonia");
            ModelState.Remove("Telefono");
            ModelState.Remove("Municipio");
            ModelState.Remove("EstadoCivil");
            ModelState.Remove("CodigoPostal");
            ModelState.Remove("Nacionalidad");
            ModelState.Remove("SistemaEstudio");
            ModelState.Remove("LugarNacimiento");
            ModelState.Remove("TipoPreparatoria");
            ModelState.Remove("EscuelaProcedencia");

            try
            {
                // 1. Validaciones de Negocio (Base de Datos)
                var preinscripcion = await _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .FirstOrDefaultAsync(p => p.Folio == vm.Folio);

                if (preinscripcion == null)
                {
                    ModelState.AddModelError("Folio", "El folio ingresado no existe.");
                    return View(vm);
                }

                if (preinscripcion.DatosPersonales?.CURP != vm.CurpValidacion)
                {
                    ModelState.AddModelError("CurpValidacion", "El CURP no coincide con el folio ingresado.");
                    return View(vm);
                }

                var inscripcionExistente = await _context.Inscripciones
                    .FirstOrDefaultAsync(i => i.PreinscripcionId == preinscripcion.Id);

                if (inscripcionExistente != null)
                {
                    ModelState.AddModelError("Folio", "Este folio ya fue utilizado para una inscripción.");
                    return View(vm);
                }

                // 2. Manejo de archivos (con sus propios catch para validaciones de formato/tamaño)
                //if (vm.ActaNacimientoFile != null)
                //{
                //    try { vm.ActaNacimientoPath = await _fileService.SavePdfAsync(vm.ActaNacimientoFile, "inscripciones"); }
                //    catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.ActaNacimientoFile), ex.Message); }
                //}
                //else
                //{
                //    ModelState.AddModelError(nameof(vm.ActaNacimientoFile), "Adjunte el acta de nacimiento en formato PDF.");
                //}

                //if (vm.CurpPdfFile != null)
                //{
                //    try { vm.CurpPdfPath = await _fileService.SavePdfAsync(vm.CurpPdfFile, "inscripciones"); }
                //    catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.CurpPdfFile), ex.Message); }
                //}
                //else
                //{
                //    ModelState.AddModelError(nameof(vm.CurpPdfFile), "Adjunte el CURP en formato PDF.");
                //}

                //if (vm.BoletaPdfFile != null)
                //{
                //    try { vm.BoletaPdfPath = await _fileService.SavePdfAsync(vm.BoletaPdfFile, "inscripciones"); }
                //    catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.BoletaPdfFile), ex.Message); }
                //}
                //else
                //{
                //    ModelState.AddModelError(nameof(vm.BoletaPdfFile), "Adjunte la boleta en formato PDF.");
                //}

                // 3. Persistencia de datos
                if (ModelState.IsValid)
                {
                    var entidad = new InscripcionEntity
                    {
                        PreinscripcionId = preinscripcion.Id,
                        CarreraSolicitada = vm.CarreraSolicitada,
                        TieneMatriculaTSU = vm.TieneMatriculaTSU,
                        MatriculaTSU = vm.MatriculaTSU,
                        ActaNacimientoPath = vm.ActaNacimientoPath,
                        CurpPdfPath = vm.CurpPdfPath,
                        BoletaPdfPath = vm.BoletaPdfPath,
                        FechaInscripcion = DateTime.Now,
                        EstadoInscripcion = EstadoRegistro.Pendiente.ToString()
                    };

                    _context.Inscripciones.Add(entidad);
                    await _context.SaveChangesAsync();

                    // Generar matrícula con el Id
                    entidad.Matricula = $"ITC-{DateTime.Now.Year}-{entidad.Id:D5}";
                    await _context.SaveChangesAsync();

                    await _context.Entry(entidad).Reference(i => i.Preinscripcion).LoadAsync();
                    await _context.Entry(entidad.Preinscripcion!).Reference(p => p.DatosPersonales).LoadAsync();
                    await _context.Entry(entidad.Preinscripcion!).Reference(p => p.Domicilio).LoadAsync();
                    await _context.Entry(entidad.Preinscripcion!).Reference(p => p.Tutor).LoadAsync();
                    await _context.Entry(entidad.Preinscripcion!).Reference(p => p.DatosEscolares).LoadAsync();

                    // Generar PDF
                    var pdfBytes = _pdfService.GenerarFichaInscripcion(entidad);

                    Response.Headers.Append("X-Matricula", entidad.Matricula);
                    Response.Headers.Append("X-Details-Url", Url.Action("Details", "Aspirantes", new { id = entidad.Id }));
                    Response.Headers.Append("Access-Control-Expose-Headers", "X-Matricula, X-Details-Url");

                    return File(pdfBytes, "application/pdf", $"Ficha_{entidad.Matricula}.pdf");
                }
            }
            catch (DbUpdateException ex)
            {
                // Error específico de base de datos
                var er = ex.InnerException;
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos. Verifique que los datos sean correctos.");
            }
            catch (Exception ex)
            {
                // Error general
                var er = ex.InnerException;
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al procesar la inscripción.");
            }

            return View(vm);
        }

        // GET: Aspirantes/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // POST: Aspirantes/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Aspirante vm)
        {
            ModelState.Remove("CurpValidacion");
            ModelState.Remove("Folio");
            ModelState.Remove("Nombre");
            ModelState.Remove("ApellidoPaterno");
            ModelState.Remove("CURP");
            ModelState.Remove("FechaNacimiento");
            ModelState.Remove("Email");

            var entidad = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            // Manejo de archivos
            if (vm.ActaNacimientoFile != null)
            {
                try
                {
                    var newPath = await _fileService.SavePdfAsync(vm.ActaNacimientoFile, "inscripciones");
                    _fileService.DeleteFile(entidad.ActaNacimientoPath);
                    entidad.ActaNacimientoPath = newPath;
                }
                catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.ActaNacimientoFile), ex.Message); }
            }

            if (vm.CurpPdfFile != null)
            {
                try
                {
                    var newPath = await _fileService.SavePdfAsync(vm.CurpPdfFile, "inscripciones");
                    _fileService.DeleteFile(entidad.CurpPdfPath);
                    entidad.CurpPdfPath = newPath;
                }
                catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.CurpPdfFile), ex.Message); }
            }

            if (vm.BoletaPdfFile != null)
            {
                try
                {
                    var newPath = await _fileService.SavePdfAsync(vm.BoletaPdfFile, "inscripciones");
                    _fileService.DeleteFile(entidad.BoletaPdfPath);
                    entidad.BoletaPdfPath = newPath;
                }
                catch (ArgumentException ex) { ModelState.AddModelError(nameof(vm.BoletaPdfFile), ex.Message); }
            }

            if (ModelState.IsValid)
            {
                entidad.CarreraSolicitada = vm.CarreraSolicitada;
                entidad.TieneMatriculaTSU = vm.TieneMatriculaTSU;
                entidad.MatriculaTSU = vm.MatriculaTSU;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // GET: Aspirantes/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // POST: Aspirantes/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entidad = await _context.Inscripciones.FindAsync(id);
            if (entidad != null)
            {
                _fileService.DeleteFile(entidad.ActaNacimientoPath);
                _fileService.DeleteFile(entidad.CurpPdfPath);
                _fileService.DeleteFile(entidad.BoletaPdfPath);
                _context.Inscripciones.Remove(entidad);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static Aspirante MapToViewModel(InscripcionEntity e) => new()
        {
            Id = e.Id,
            Folio = e.Preinscripcion?.Folio ?? string.Empty,
            CarreraSolicitada = e.CarreraSolicitada,
            TieneMatriculaTSU = e.TieneMatriculaTSU,
            MatriculaTSU = e.MatriculaTSU,
            Matricula = e.Matricula,
            ActaNacimientoPath = e.ActaNacimientoPath,
            CurpPdfPath = e.CurpPdfPath,
            BoletaPdfPath = e.BoletaPdfPath,
            FechaRegistro = e.FechaInscripcion,
            EstadoRegistro = Enum.TryParse<EstadoRegistro>(e.EstadoInscripcion, out var estado)
                             ? estado : EstadoRegistro.Pendiente,

            // Datos personales desde preinscripción
            Nombre = e.Preinscripcion?.DatosPersonales?.Nombre,
            ApellidoPaterno = e.Preinscripcion?.DatosPersonales?.ApellidoPaterno,
            ApellidoMaterno = e.Preinscripcion?.DatosPersonales?.ApellidoMaterno,
            CURP = e.Preinscripcion?.DatosPersonales?.CURP,
            FechaNacimiento = e.Preinscripcion?.DatosPersonales?.FechaNacimiento ?? default,
            Email = e.Preinscripcion?.DatosPersonales?.Email
        };

        private bool InscripcionExists(int id) =>
            _context.Inscripciones.Any(e => e.Id == id);
    }
}