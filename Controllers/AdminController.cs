using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Admin,Administrator,Master")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var preinscripciones = await _context.Preinscripciones
                .Include(p => p.DatosPersonales)
                .Include(p => p.Domicilio)
                .OrderBy(p => p.academiccontrol_preinscription_state == "Pendiente" ? 0 : 1)
                .ThenByDescending(p => p.academiccontrol_preinscription_registrationDate)
                .ToListAsync();

            var aspirantes = await _context.Inscripciones
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.DatosPersonales)
                .Include(i => i.Preinscripcion)
                    .ThenInclude(p => p.Domicilio)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                var preinscripciones = await _context.Preinscripciones
                    .Include(p => p.DatosPersonales)
                    .Include(p => p.Domicilio)
                    .ToListAsync();

                PreinscripcionesPendientes = preinscripciones.Count(p => p.academiccontrol_preinscription_state == "Pendiente"),
                PreinscripcionesConvertidas = preinscripciones.Count(p => p.academiccontrol_preinscription_state == "Convertida"),
                PreinscripcionesCanceladas = preinscripciones.Count(p => p.academiccontrol_preinscription_state == "Cancelada"),

                AspirantesPendientes = aspirantes.Count(a => a.academiccontrol_inscription_state == "Pendiente"),
                AspirantesAprobados = aspirantes.Count(a => a.academiccontrol_inscription_state == "Aprobado"),
                AspirantesRechazados = aspirantes.Count(a => a.academiccontrol_inscription_state == "Rechazado"),

                PreinscripcionesPorCarrera = preinscripciones
                    .GroupBy(p => p.academiccontrol_preinscription_careerRequested)
                    .ToDictionary(g => g.Key, g => g.Count()),

                AspirantesPorCarrera = aspirantes
                    .GroupBy(a => a.academiccontrol_inscription_careerRequested)
                    .ToDictionary(g => g.Key, g => g.Count()),

                PreinscripcionesPorEstado = preinscripciones
                    .Where(p => !string.IsNullOrEmpty(p.Domicilio?.academiccontrol_preinscription_address_state))
                    .GroupBy(p => p.Domicilio!.academiccontrol_preinscription_address_state)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),

                AspirantesPorEstado = aspirantes
                    .Where(a => !string.IsNullOrEmpty(a.Preinscripcion?.Domicilio?.academiccontrol_preinscription_address_state))
                    .GroupBy(a => a.Preinscripcion!.Domicilio!.academiccontrol_preinscription_address_state)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),

                UltimasPreinscripciones = preinscripciones
                    .OrderByDescending(p => p.academiccontrol_preinscription_registrationDate)
                    .Take(10)
                    .ToList(),

                UltimosAspirantes = aspirantes
                    .OrderByDescending(a => a.academiccontrol_inscription_registrationDate)
                    .Take(10)
                    .ToList()
            };

                    UltimasPreinscripciones = preinscripciones
                        .OrderByDescending(p => p.FechaPreinscripcion)
                        .Take(10)
                        .ToList(),

                    UltimosAspirantes = aspirantes
                        .OrderByDescending(a => a.FechaInscripcion)
                        .Take(10)
                        .ToList()
                };

                return View(vm);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogWarning(ex, "Admin dashboard: faltan tablas legacy de preinscripcion/inscripcion en la base de datos activa.");
                TempData["WarningMessage"] = "La base de datos activa no tiene tablas de preinscripcion/inscripcion; el dashboard admin se muestra sin esos datos.";
                return View(new AdminDashboardViewModel());
            }
        }
    }
}