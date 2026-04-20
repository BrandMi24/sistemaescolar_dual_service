using ControlEscolar.Data;
using ControlEscolar.Enums;
using ControlEscolar.Models;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Preinscripciones,Admisiones,Administrativo,Coordinador,Director,Admin,Administrator,Master")]
    public class PreinscripcionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;

        public PreinscripcionesController(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var entidades = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .OrderBy(p => p.academiccontrol_preinscription_state == "Pendiente" ? 0 : 1)
                .ThenByDescending(p => p.academiccontrol_preinscription_registrationDate)
                .ToListAsync();

            return View(entidades.Select(e => MapToViewModel(e)).ToList());
        }

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
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        public async Task<IActionResult> Create()
        {
            var hoy = DateTime.Today;

            var configuraciones = await _context.ConfiguracionFichas
                .Where(c => c.academiccontrol_inscription_ticketconfig_status
                         && c.academiccontrol_inscription_ticketconfig_startDate <= hoy
                         && c.academiccontrol_inscription_ticketconfig_endDate >= hoy)
                .ToListAsync();

            var carrerasDisponibles = new List<string>();

            foreach (var config in configuraciones)
            {
                var fichasUsadas = await _context.Preinscripciones
                    .CountAsync(p => p.academiccontrol_preinscription_careerRequested ==
                                     config.academiccontrol_inscription_ticketconfig_career
                                  && p.academiccontrol_preinscription_registrationDate >=
                                     config.academiccontrol_inscription_ticketconfig_startDate
                                  && p.academiccontrol_preinscription_registrationDate <=
                                     config.academiccontrol_inscription_ticketconfig_endDate);

                if (fichasUsadas < config.academiccontrol_inscription_ticketconfig_limit)
                {
                    carrerasDisponibles.Add(config.academiccontrol_inscription_ticketconfig_career);
                }
            }

            ViewBag.CarrerasDisponibles = carrerasDisponibles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Preinscripcion vm)
        {

            // Validar unicidad de CURP en el periodo activo actual
            var activePeriodConfig = await _context.PeriodosInscripcion
                .FirstOrDefaultAsync(p => p.academiccontrol_inscription_period_status
                            && p.academiccontrol_inscription_period_startDate <= DateTime.Today
                            && p.academiccontrol_inscription_period_endDate >= DateTime.Today);
                            
            if (activePeriodConfig != null)
            {
                var curpExistente = await _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .AnyAsync(p => p.DatosPersonales!.academiccontrol_preinscription_personaldata_CURP == vm.academiccontrol_preinscription_personaldata_CURP
                                && p.academiccontrol_preinscription_registrationDate >= activePeriodConfig.academiccontrol_inscription_period_startDate
                                && p.academiccontrol_preinscription_registrationDate <= activePeriodConfig.academiccontrol_inscription_period_endDate);

                if (curpExistente)
                {
                    return Json(new { success = false, message = "El CURP ingresado ya se encuentra registrado en el periodo actual." });
                }
            }

            // Validar que exista configuración activa para la carrera y que esté dentro del periodo
            var config = await _context.ConfiguracionFichas
                .FirstOrDefaultAsync(c => c.academiccontrol_inscription_ticketconfig_career == vm.academiccontrol_preinscription_careerRequested
                                       && c.academiccontrol_inscription_ticketconfig_status
                                       && c.academiccontrol_inscription_ticketconfig_startDate <= DateTime.Today
                                       && c.academiccontrol_inscription_ticketconfig_endDate >= DateTime.Today);

            if (config == null)
            {
                return Json(new { success = false, message = $"El periodo de preinscripción para '{vm.academiccontrol_preinscription_careerRequested}' no está activo en este momento." });
            }

            var fichasUsadas = await _context.Preinscripciones
                .CountAsync(p => p.academiccontrol_preinscription_careerRequested == vm.academiccontrol_preinscription_careerRequested
                              && p.academiccontrol_preinscription_registrationDate >= config.academiccontrol_inscription_ticketconfig_startDate
                              && p.academiccontrol_preinscription_registrationDate <= config.academiccontrol_inscription_ticketconfig_endDate);

            if (fichasUsadas >= config.academiccontrol_inscription_ticketconfig_limit)
            {
                return Json(new { success = false, message = $"Cupo completo para '{vm.academiccontrol_preinscription_careerRequested}'." });
            }

            // ── Validación de coherencia de negocio (doble seguro servidor) ──────────────

            // Edad mínima: 15 años
            if (vm.academiccontrol_preinscription_personaldata_birthDate != default)
            {
                var edadAspirante = (int)Math.Floor(
                    (DateTime.Today - vm.academiccontrol_preinscription_personaldata_birthDate).TotalDays / 365.25);

                if (edadAspirante < 15)
                    ModelState.AddModelError(
                        nameof(vm.academiccontrol_preinscription_personaldata_birthDate),
                        "El aspirante debe tener al menos 15 años de edad.");
            }

            // Intervalo de secundaria: exactamente 3 años (±30 días = 1065–1125 días)
            if (vm.academiccontrol_preinscription_academic_startDate.HasValue &&
                vm.academiccontrol_preinscription_academic_endDate.HasValue)
            {
                var inicioSec = vm.academiccontrol_preinscription_academic_startDate.Value;
                var egresoSec = vm.academiccontrol_preinscription_academic_endDate.Value;

                if (egresoSec <= inicioSec)
                {
                    ModelState.AddModelError(
                        nameof(vm.academiccontrol_preinscription_academic_endDate),
                        "La fecha de egreso de secundaria debe ser posterior a la fecha de inicio.");
                }
                else
                {
                    var diffDias = (egresoSec - inicioSec).TotalDays;
                    if (diffDias < 1065 || diffDias > 1125)
                        ModelState.AddModelError(
                            nameof(vm.academiccontrol_preinscription_academic_endDate),
                            "El intervalo entre inicio y egreso de secundaria debe ser de exactamente 3 años.");
                }
            }

            // ─────────────────────────────────────────────────────────────────────────────

            if (ModelState.IsValid)
            {
                try
                {
                    var entidad = new PreinscripcionEntity
                    {
                        academiccontrol_preinscription_careerRequested = vm.academiccontrol_preinscription_careerRequested,
                        academiccontrol_preinscription_average = vm.academiccontrol_preinscription_average ?? 0,
                        academiccontrol_preinscription_diffusionMedia = vm.academiccontrol_preinscription_diffusionMedia,
                        academiccontrol_preinscription_registrationDate = DateTime.Now,
                        academiccontrol_preinscription_state = "Pendiente",
                        academiccontrol_preinscription_createdDate = DateTime.Now,

                        DatosPersonales = new PreinscripcionDatosPersonalesEntity
                        {
                            academiccontrol_preinscription_personaldata_name = vm.academiccontrol_preinscription_personaldata_name,
                            academiccontrol_preinscription_personaldata_paternalSurname = vm.academiccontrol_preinscription_personaldata_paternalSurname,
                            academiccontrol_preinscription_personaldata_maternalSurname = vm.academiccontrol_preinscription_personaldata_maternalSurname,
                            academiccontrol_preinscription_personaldata_CURP = vm.academiccontrol_preinscription_personaldata_CURP,
                            academiccontrol_preinscription_personaldata_birthDate = vm.academiccontrol_preinscription_personaldata_birthDate,
                            academiccontrol_preinscription_personaldata_gender = vm.academiccontrol_preinscription_personaldata_gender,
                            academiccontrol_preinscription_personaldata_maritalStatus = vm.academiccontrol_preinscription_personaldata_maritalStatus,
                            academiccontrol_preinscription_personaldata_email = vm.academiccontrol_preinscription_personaldata_email,
                            academiccontrol_preinscription_personaldata_phone = vm.academiccontrol_preinscription_personaldata_phone,
                            academiccontrol_preinscription_personaldata_createdDate = DateTime.Now
                        },

                        Domicilio = new PreinscripcionDomicilioEntity
                        {
                            academiccontrol_preinscription_address_state = vm.academiccontrol_preinscription_address_state,
                            academiccontrol_preinscription_address_municipality = vm.academiccontrol_preinscription_address_municipality,
                            academiccontrol_preinscription_address_zipCode = vm.academiccontrol_preinscription_address_zipCode,
                            academiccontrol_preinscription_address_neighborhood = vm.academiccontrol_preinscription_address_neighborhood,
                            academiccontrol_preinscription_address_street = vm.academiccontrol_preinscription_address_street,
                            academiccontrol_preinscription_address_exteriorNumber = vm.academiccontrol_preinscription_address_exteriorNumber,
                            academiccontrol_preinscription_address_createdDate = DateTime.Now
                        },

                        Tutor = new PreinscripcionTutorEntity
                        {
                            academiccontrol_preinscription_tutor_fullName = vm.academiccontrol_preinscription_tutor_fullName,
                            academiccontrol_preinscription_tutor_relationship = vm.academiccontrol_preinscription_tutor_relationship,
                            academiccontrol_preinscription_tutor_phone = vm.academiccontrol_preinscription_tutor_phone,
                            academiccontrol_preinscription_tutor_createdDate = DateTime.Now
                        },

                        DatosEscolares = new PreinscripcionEscolarEntity
                        {
                            academiccontrol_preinscription_academic_originSchool = vm.academiccontrol_preinscription_academic_originSchool,
                            academiccontrol_preinscription_academic_schoolState = vm.academiccontrol_preinscription_academic_schoolState,
                            academiccontrol_preinscription_academic_schoolMunicipality = vm.academiccontrol_preinscription_academic_schoolMunicipality,
                            academiccontrol_preinscription_academic_CCT = vm.academiccontrol_preinscription_academic_CCT,
                            academiccontrol_preinscription_academic_startDate = vm.academiccontrol_preinscription_academic_startDate,
                            academiccontrol_preinscription_academic_endDate = vm.academiccontrol_preinscription_academic_endDate,
                            academiccontrol_preinscription_academic_createdDate = DateTime.Now
                        },

                        Salud = new PreinscripcionSaludEntity
                        {
                            academiccontrol_preinscription_health_medicalService = vm.academiccontrol_preinscription_health_medicalService,
                            academiccontrol_preinscription_health_hasDisability = vm.academiccontrol_preinscription_health_hasDisability,
                            academiccontrol_preinscription_health_disabilityDescription = vm.academiccontrol_preinscription_health_disabilityDescription,
                            academiccontrol_preinscription_health_indigenousCommunity = vm.academiccontrol_preinscription_health_indigenousCommunity,
                            academiccontrol_preinscription_health_indigenousCommunityDescription = vm.academiccontrol_preinscription_health_indigenousCommunityDescription,
                            academiccontrol_preinscription_health_comments = vm.academiccontrol_preinscription_health_comments,
                            academiccontrol_preinscription_health_hasChildren = vm.academiccontrol_preinscription_health_hasChildren,
                            academiccontrol_preinscription_health_createdDate = DateTime.Now
                        }
                    };

                    _context.Preinscripciones.Add(entidad);
                    await _context.SaveChangesAsync();

                    entidad.academiccontrol_preinscription_folio = $"PRE-{DateTime.Now.Year}-{entidad.academiccontrol_preinscription_ID:D5}";
                    await _context.SaveChangesAsync();

                    // Carga para PDF
                    await _context.Entry(entidad).Reference(p => p.DatosPersonales).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Domicilio).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Tutor).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.DatosEscolares).LoadAsync();
                    await _context.Entry(entidad).Reference(p => p.Salud).LoadAsync();

                    var pdfBytes = _pdfService.GenerarFichaPreinscripcion(entidad);

                    TempData["SuccessMessage"] = $"Registro exitoso. Folio: {entidad.academiccontrol_preinscription_folio}";

                    Response.Headers.Append("X-Folio", entidad.academiccontrol_preinscription_folio);
                    Response.Headers.Append("X-Details-Url", Url.Action("Details", "Preinscripciones", new { id = entidad.academiccontrol_preinscription_ID }));
                    Response.Headers.Append("Access-Control-Expose-Headers", "X-Folio, X-Details-Url");

                    return File(pdfBytes, "application/pdf", $"Ficha_{entidad.academiccontrol_preinscription_folio}.pdf");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Error al procesar la preinscripción.");
                }
            }
            return View(vm);
        }

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
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            return View(MapToViewModel(entidad));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Preinscripcion vm)
        {
            var entidad = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .Include(p => p.Tutor)
                .Include(p => p.DatosEscolares)
                .Include(p => p.Salud)
                .FirstOrDefaultAsync(p => p.academiccontrol_preinscription_ID == id);

            if (entidad == null) return NotFound();

            if (ModelState.IsValid)
            {
                entidad.academiccontrol_preinscription_careerRequested = vm.academiccontrol_preinscription_careerRequested;
                entidad.academiccontrol_preinscription_average = vm.academiccontrol_preinscription_average ?? 0;
                entidad.academiccontrol_preinscription_diffusionMedia = vm.academiccontrol_preinscription_diffusionMedia;

                if (entidad.DatosPersonales != null)
                {
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_name = vm.academiccontrol_preinscription_personaldata_name;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_paternalSurname = vm.academiccontrol_preinscription_personaldata_paternalSurname;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_maternalSurname = vm.academiccontrol_preinscription_personaldata_maternalSurname;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_CURP = vm.academiccontrol_preinscription_personaldata_CURP;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_email = vm.academiccontrol_preinscription_personaldata_email;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_phone = vm.academiccontrol_preinscription_personaldata_phone;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_birthDate = vm.academiccontrol_preinscription_personaldata_birthDate;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_gender = vm.academiccontrol_preinscription_personaldata_gender;
                    entidad.DatosPersonales.academiccontrol_preinscription_personaldata_maritalStatus = vm.academiccontrol_preinscription_personaldata_maritalStatus;
                }

                if (entidad.Domicilio != null)
                {
                    entidad.Domicilio.academiccontrol_preinscription_address_state = vm.academiccontrol_preinscription_address_state;
                    entidad.Domicilio.academiccontrol_preinscription_address_municipality = vm.academiccontrol_preinscription_address_municipality;
                    entidad.Domicilio.academiccontrol_preinscription_address_zipCode = vm.academiccontrol_preinscription_address_zipCode;
                    entidad.Domicilio.academiccontrol_preinscription_address_neighborhood = vm.academiccontrol_preinscription_address_neighborhood;
                    entidad.Domicilio.academiccontrol_preinscription_address_street = vm.academiccontrol_preinscription_address_street;
                    entidad.Domicilio.academiccontrol_preinscription_address_exteriorNumber = vm.academiccontrol_preinscription_address_exteriorNumber;
                }

                if (entidad.Tutor != null)
                {
                    entidad.Tutor.academiccontrol_preinscription_tutor_fullName = vm.academiccontrol_preinscription_tutor_fullName;
                    entidad.Tutor.academiccontrol_preinscription_tutor_relationship = vm.academiccontrol_preinscription_tutor_relationship;
                    entidad.Tutor.academiccontrol_preinscription_tutor_phone = vm.academiccontrol_preinscription_tutor_phone;
                }

                if (entidad.DatosEscolares != null)
                {
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_originSchool = vm.academiccontrol_preinscription_academic_originSchool;
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_schoolState = vm.academiccontrol_preinscription_academic_schoolState;
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_schoolMunicipality = vm.academiccontrol_preinscription_academic_schoolMunicipality;
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_CCT = vm.academiccontrol_preinscription_academic_CCT;
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_startDate = vm.academiccontrol_preinscription_academic_startDate;
                    entidad.DatosEscolares.academiccontrol_preinscription_academic_endDate = vm.academiccontrol_preinscription_academic_endDate;
                }

                if (entidad.Salud != null)
                {
                    entidad.Salud.academiccontrol_preinscription_health_medicalService = vm.academiccontrol_preinscription_health_medicalService;
                    entidad.Salud.academiccontrol_preinscription_health_hasDisability = vm.academiccontrol_preinscription_health_hasDisability;
                    entidad.Salud.academiccontrol_preinscription_health_disabilityDescription = vm.academiccontrol_preinscription_health_disabilityDescription;
                    entidad.Salud.academiccontrol_preinscription_health_indigenousCommunity = vm.academiccontrol_preinscription_health_indigenousCommunity;
                    entidad.Salud.academiccontrol_preinscription_health_indigenousCommunityDescription = vm.academiccontrol_preinscription_health_indigenousCommunityDescription;
                    entidad.Salud.academiccontrol_preinscription_health_comments = vm.academiccontrol_preinscription_health_comments;
                    entidad.Salud.academiccontrol_preinscription_health_hasChildren = vm.academiccontrol_preinscription_health_hasChildren;
                }

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
            var entidad = await _context.Preinscripciones.FindAsync(id);
            if (entidad != null) _context.Preinscripciones.Remove(entidad);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static Preinscripcion MapToViewModel(PreinscripcionEntity e) => new()
        {
            academiccontrol_preinscription_ID = e.academiccontrol_preinscription_ID,
            academiccontrol_preinscription_folio = e.academiccontrol_preinscription_folio,
            academiccontrol_preinscription_careerRequested = e.academiccontrol_preinscription_careerRequested,
            academiccontrol_preinscription_average = e.academiccontrol_preinscription_average,
            academiccontrol_preinscription_diffusionMedia = e.academiccontrol_preinscription_diffusionMedia,
            academiccontrol_preinscription_registrationDate = e.academiccontrol_preinscription_registrationDate,

            academiccontrol_preinscription_personaldata_name = e.DatosPersonales?.academiccontrol_preinscription_personaldata_name ?? "",
            academiccontrol_preinscription_personaldata_paternalSurname = e.DatosPersonales?.academiccontrol_preinscription_personaldata_paternalSurname ?? "",
            academiccontrol_preinscription_personaldata_maternalSurname = e.DatosPersonales?.academiccontrol_preinscription_personaldata_maternalSurname,
            academiccontrol_preinscription_personaldata_CURP = e.DatosPersonales?.academiccontrol_preinscription_personaldata_CURP ?? "",
            academiccontrol_preinscription_personaldata_email = e.DatosPersonales?.academiccontrol_preinscription_personaldata_email ?? "",
            academiccontrol_preinscription_personaldata_phone = e.DatosPersonales?.academiccontrol_preinscription_personaldata_phone,
            academiccontrol_preinscription_personaldata_birthDate = e.DatosPersonales?.academiccontrol_preinscription_personaldata_birthDate ?? default,
            academiccontrol_preinscription_personaldata_gender = e.DatosPersonales?.academiccontrol_preinscription_personaldata_gender ?? "",
            academiccontrol_preinscription_personaldata_maritalStatus = e.DatosPersonales?.academiccontrol_preinscription_personaldata_maritalStatus,

            academiccontrol_preinscription_address_state = e.Domicilio?.academiccontrol_preinscription_address_state ?? "",
            academiccontrol_preinscription_address_municipality = e.Domicilio?.academiccontrol_preinscription_address_municipality ?? "",
            academiccontrol_preinscription_address_zipCode = e.Domicilio?.academiccontrol_preinscription_address_zipCode,
            academiccontrol_preinscription_address_neighborhood = e.Domicilio?.academiccontrol_preinscription_address_neighborhood ?? "",
            academiccontrol_preinscription_address_street = e.Domicilio?.academiccontrol_preinscription_address_street ?? "",
            academiccontrol_preinscription_address_exteriorNumber = e.Domicilio?.academiccontrol_preinscription_address_exteriorNumber ?? "",

            academiccontrol_preinscription_tutor_fullName = e.Tutor?.academiccontrol_preinscription_tutor_fullName ?? "",
            academiccontrol_preinscription_tutor_relationship = e.Tutor?.academiccontrol_preinscription_tutor_relationship ?? "",
            academiccontrol_preinscription_tutor_phone = e.Tutor?.academiccontrol_preinscription_tutor_phone ?? "",

            academiccontrol_preinscription_academic_originSchool = e.DatosEscolares?.academiccontrol_preinscription_academic_originSchool ?? "",
            academiccontrol_preinscription_academic_schoolState = e.DatosEscolares?.academiccontrol_preinscription_academic_schoolState,
            academiccontrol_preinscription_academic_schoolMunicipality = e.DatosEscolares?.academiccontrol_preinscription_academic_schoolMunicipality,
            academiccontrol_preinscription_academic_CCT = e.DatosEscolares?.academiccontrol_preinscription_academic_CCT,
            academiccontrol_preinscription_academic_startDate = e.DatosEscolares?.academiccontrol_preinscription_academic_startDate,
            academiccontrol_preinscription_academic_endDate = e.DatosEscolares?.academiccontrol_preinscription_academic_endDate,

            academiccontrol_preinscription_health_medicalService = e.Salud?.academiccontrol_preinscription_health_medicalService,
            academiccontrol_preinscription_health_hasDisability = e.Salud?.academiccontrol_preinscription_health_hasDisability ?? false,
            academiccontrol_preinscription_health_disabilityDescription = e.Salud?.academiccontrol_preinscription_health_disabilityDescription,
            academiccontrol_preinscription_health_indigenousCommunity = e.Salud?.academiccontrol_preinscription_health_indigenousCommunity ?? false,
            academiccontrol_preinscription_health_indigenousCommunityDescription = e.Salud?.academiccontrol_preinscription_health_indigenousCommunityDescription,
            academiccontrol_preinscription_health_comments = e.Salud?.academiccontrol_preinscription_health_comments,
            academiccontrol_preinscription_health_hasChildren = e.Salud?.academiccontrol_preinscription_health_hasChildren ?? false,

            academiccontrol_preinscription_state = e.academiccontrol_preinscription_state,
            MotivoRechazo = e.academiccontrol_preinscription_rejectionReason
        };
    }
}