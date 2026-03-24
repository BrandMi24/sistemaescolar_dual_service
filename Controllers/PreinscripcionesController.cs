using ControlEscolar.Data;
using ControlEscolar.Enums;
using ControlEscolar.Models;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Controllers
{
    public class PreinscripcionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;


        public PreinscripcionesController(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: Preinscripciones
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var entidades = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .ToListAsync();

            return View(entidades.Select(e => MapToViewModel(e)).ToList());
        }

        // GET: Preinscripciones/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.Tutor)
                .Include(p => p.DatosEscolares)
                .Include(p => p.Salud)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // GET: Preinscripciones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Preinscripciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Preinscripcion vm)
        {
            ModelState.Remove("Telefono");
            ModelState.Remove("Division");
            ModelState.Remove("OpcionEducativa");
            ModelState.Remove("Localidad");

            // Validar límite de fichas por carrera 
            var config = await _context.ConfiguracionFichas
                .FirstOrDefaultAsync(c => c.Carrera == vm.CarreraSolicitada
                                       && c.Activo
                                       && c.FechaInicio <= DateTime.Today
                                       && c.FechaFin >= DateTime.Today);

            if (config != null)
            {
                var fichasUsadas = await _context.Preinscripciones
                    .CountAsync(p => p.CarreraSolicitada == vm.CarreraSolicitada
                                  && p.FechaPreinscripcion >= config.FechaInicio
                                  && p.FechaPreinscripcion <= config.FechaFin);

                if (fichasUsadas >= config.LimiteFichas)
                {
                    TempData["ErrorMessage"] = $"Lo sentimos, el cupo para la carrera '{vm.CarreraSolicitada}' ha sido completado. No hay fichas disponibles.";
                    return View(vm);
                }
            }

            // Validar periodo de inscripción activo 
            var periodoActivo = await _context.PeriodosInscripcion
                .AnyAsync(p => p.Activo
                            && p.FechaInicio <= DateTime.Today
                            && p.FechaFin >= DateTime.Today);

            if (!periodoActivo)
            {
                TempData["ErrorMessage"] = "Lo sentimos, el periodo de preinscripción no está activo en este momento.";
                return View(vm);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var entidad = new PreinscripcionEntity
                    {
                        CarreraSolicitada = vm.CarreraSolicitada,
                        Promedio = vm.Promedio ?? 0,
                        MedioDifusion = vm.MedioDifusion,
                        FechaPreinscripcion = DateTime.Now,
                        EstadoPreinscripcion = EstadoPreinscripcion.Pendiente.ToString(),

                        DatosPersonales = new PreinscripcionDatosPersonalesEntity
                        {
                            Nombre = vm.Nombre,
                            ApellidoPaterno = vm.ApellidoPaterno,
                            ApellidoMaterno = vm.ApellidoMaterno,
                            CURP = vm.CURP,
                            FechaNacimiento = vm.FechaNacimiento,
                            Sexo = vm.Sexo,
                            EstadoCivil = vm.EstadoCivil,
                            Email = vm.Email,
                            Telefono = vm.Telefono
                        },

                        Domicilio = new PreinscripcionDomicilioEntity
                        {
                            Estado = vm.Estado,
                            Municipio = vm.Municipio,
                            CodigoPostal = vm.CodigoPostal,
                            Colonia = vm.Colonia,
                            Calle = vm.Calle,
                            NumeroExterior = vm.NumeroExterior
                        },

                        Tutor = new PreinscripcionTutorEntity
                        {
                            TutorNombre = vm.TutorNombre ?? string.Empty,
                            Parentesco = vm.Parentesco ?? string.Empty,
                            Telefono = vm.TelefonoEmergencia ?? string.Empty
                        },

                        DatosEscolares = new PreinscripcionEscolarEntity
                        {
                            EscuelaProcedencia = vm.EscuelaProcedencia ?? string.Empty,
                            EstadoEscuela = vm.EstadoEscuela,
                            MunicipioEscuela = vm.MunicipioEscuela,
                            CCT = vm.CCT,
                            InicioBachillerato = vm.InicioBachillerato,
                            FinBachillerato = vm.FinBachillerato
                        },

                        Salud = new PreinscripcionSaludEntity
                        {
                            ServicioMedico = vm.ServicioMedico,
                            TieneDiscapacidad = vm.TieneDiscapacidad,
                            DiscapacidadDescripcion = vm.DiscapacidadDescripcion,
                            ComunidadIndigena = vm.ComunidadIndigena,
                            ComunidadIndigenaDescripcion = vm.ComunidadIndigenaDescripcion,
                            Comentarios = vm.Comentarios
                        }
                    };

                    _context.Preinscripciones.Add(entidad);
                    await _context.SaveChangesAsync();

                    entidad.Folio = $"PRE-{DateTime.Now.Year}-{entidad.Id:D5}";
                    await _context.SaveChangesAsync();

                    await _context.Entry(entidad).Reference(p => p.DatosPersonales).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Domicilio).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Tutor).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.DatosEscolares).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Salud).LoadAsync();

                    var pdfBytes = _pdfService.GenerarFichaPreinscripcion(entidad);

                    TempData["SuccessMessage"] = $"Preinscripción registrada. Folio: {entidad.Folio}";

                    Response.Headers.Append("X-Folio", entidad.Folio);
                    Response.Headers.Append("X-Details-Url", Url.Action("Details", "Preinscripciones", new { id = entidad.Id }));
                    Response.Headers.Append("Access-Control-Expose-Headers", "X-Folio, X-Details-Url");
                    return File(pdfBytes, "application/pdf", $"Ficha_{entidad.Folio}.pdf");
                }
                catch (Exception ex)
                {
                    var error = ex.InnerException;
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al procesar tu solicitud. Por favor, inténtalo de nuevo.");


                }
            }

            return View(vm);
        }

        // GET: Preinscripciones/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.Tutor)
                .Include(p => p.DatosEscolares)
                .Include(p => p.Salud)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // POST: Preinscripciones/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Preinscripcion vm)
        {
            ModelState.Remove("Telefono");
            ModelState.Remove("Division");
            ModelState.Remove("OpcionEducativa");
            ModelState.Remove("Localidad");

            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.Tutor)
                .Include(p => p.DatosEscolares)
                .Include(p => p.Salud)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            if (ModelState.IsValid)
            {
                entidad.CarreraSolicitada = vm.CarreraSolicitada;
                entidad.Promedio = vm.Promedio ?? 0;
                entidad.MedioDifusion = vm.MedioDifusion;

                entidad.DatosPersonales!.Nombre = vm.Nombre;
                entidad.DatosPersonales.ApellidoPaterno = vm.ApellidoPaterno;
                entidad.DatosPersonales.ApellidoMaterno = vm.ApellidoMaterno;
                entidad.DatosPersonales.CURP = vm.CURP;
                entidad.DatosPersonales.FechaNacimiento = vm.FechaNacimiento;
                entidad.DatosPersonales.Sexo = vm.Sexo;
                entidad.DatosPersonales.EstadoCivil = vm.EstadoCivil;
                entidad.DatosPersonales.Email = vm.Email;
                entidad.DatosPersonales.Telefono = vm.Telefono;

                entidad.Domicilio!.Estado = vm.Estado;
                entidad.Domicilio.Municipio = vm.Municipio;
                entidad.Domicilio.CodigoPostal = vm.CodigoPostal;
                entidad.Domicilio.Colonia = vm.Colonia;
                entidad.Domicilio.Calle = vm.Calle;
                entidad.Domicilio.NumeroExterior = vm.NumeroExterior;

                entidad.Tutor!.TutorNombre = vm.TutorNombre ?? string.Empty;
                entidad.Tutor.Parentesco = vm.Parentesco ?? string.Empty;
                entidad.Tutor.Telefono = vm.TelefonoEmergencia ?? string.Empty;

                entidad.DatosEscolares!.EscuelaProcedencia = vm.EscuelaProcedencia ?? string.Empty;
                entidad.DatosEscolares.EstadoEscuela = vm.EstadoEscuela;
                entidad.DatosEscolares.MunicipioEscuela = vm.MunicipioEscuela;
                entidad.DatosEscolares.CCT = vm.CCT;
                entidad.DatosEscolares.InicioBachillerato = vm.InicioBachillerato;
                entidad.DatosEscolares.FinBachillerato = vm.FinBachillerato;

                entidad.Salud!.ServicioMedico = vm.ServicioMedico;
                entidad.Salud.TieneDiscapacidad = vm.TieneDiscapacidad;
                entidad.Salud.DiscapacidadDescripcion = vm.DiscapacidadDescripcion;
                entidad.Salud.ComunidadIndigena = vm.ComunidadIndigena;
                entidad.Salud.ComunidadIndigenaDescripcion = vm.ComunidadIndigenaDescripcion;
                entidad.Salud.Comentarios = vm.Comentarios;
                //entidad.Salud.TieneHijos = vm.TieneHijos;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // GET: Preinscripciones/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        // POST: Preinscripciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entidad = await _context.Preinscripciones.FindAsync(id);
            if (entidad != null)
                _context.Preinscripciones.Remove(entidad);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    
        private static Preinscripcion MapToViewModel(PreinscripcionEntity e) => new()
        {
            Id = e.Id,
            Folio = e.Folio,
            CarreraSolicitada = e.CarreraSolicitada,
            Promedio = e.Promedio,
            MedioDifusion = e.MedioDifusion,
            FechaPreinscripcion = e.FechaPreinscripcion,
            EstadoPreinscripcion = Enum.TryParse<EstadoPreinscripcion>(e.EstadoPreinscripcion, out var estado)
                                   ? estado : EstadoPreinscripcion.Pendiente,

            Nombre = e.DatosPersonales?.Nombre ?? string.Empty,
            ApellidoPaterno = e.DatosPersonales?.ApellidoPaterno ?? string.Empty,
            ApellidoMaterno = e.DatosPersonales?.ApellidoMaterno,
            CURP = e.DatosPersonales?.CURP ?? string.Empty,
            FechaNacimiento = e.DatosPersonales?.FechaNacimiento ?? default,
            Sexo = e.DatosPersonales?.Sexo ?? string.Empty,
            EstadoCivil = e.DatosPersonales?.EstadoCivil,
            Email = e.DatosPersonales?.Email ?? string.Empty,
            Telefono = e.DatosPersonales?.Telefono,

            Estado = e.Domicilio?.Estado ?? string.Empty,
            Municipio = e.Domicilio?.Municipio ?? string.Empty,
            CodigoPostal = e.Domicilio?.CodigoPostal,
            Colonia = e.Domicilio?.Colonia ?? string.Empty,
            Calle = e.Domicilio?.Calle ?? string.Empty,
            NumeroExterior = e.Domicilio?.NumeroExterior ?? string.Empty,

            TutorNombre = e.Tutor?.TutorNombre,
            Parentesco = e.Tutor?.Parentesco,
            TelefonoEmergencia = e.Tutor?.Telefono,

            EscuelaProcedencia = e.DatosEscolares?.EscuelaProcedencia,
            EstadoEscuela = e.DatosEscolares?.EstadoEscuela,
            MunicipioEscuela = e.DatosEscolares?.MunicipioEscuela,
            CCT = e.DatosEscolares?.CCT,
            InicioBachillerato = e.DatosEscolares?.InicioBachillerato,
            FinBachillerato = e.DatosEscolares?.FinBachillerato,

            ServicioMedico = e.Salud?.ServicioMedico,
            TieneDiscapacidad = e.Salud?.TieneDiscapacidad ?? false,
            DiscapacidadDescripcion = e.Salud?.DiscapacidadDescripcion,
            ComunidadIndigena = e.Salud?.ComunidadIndigena ?? false,
            ComunidadIndigenaDescripcion = e.Salud?.ComunidadIndigenaDescripcion,
            Comentarios = e.Salud?.Comentarios,
            //TieneHijos = e.Salud?.TieneHijos ?? false
        };

        private bool PreinscripcionExists(int id) =>
            _context.Preinscripciones.Any(e => e.Id == id);
    }
}