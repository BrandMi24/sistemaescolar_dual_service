using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
                TotalPreinscripciones = preinscripciones.Count,
                TotalAspirantes = aspirantes.Count,

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

            return View(vm);
        }
    }
}