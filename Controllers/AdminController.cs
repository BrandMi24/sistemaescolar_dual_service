//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ControlEscolar.Data;
//using ControlEscolar.Enums;
//using ControlEscolar.Models;

//namespace ControlEscolar.Controllers
//{
//    //[Authorize]
//    public class AdminController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public AdminController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var preinscripciones = await _context.Preinscripciones.ToListAsync();
//            var aspirantes = await _context.Aspirantes.ToListAsync();

//            var vm = new AdminDashboardViewModel
//            {
//                TotalPreinscripciones = preinscripciones.Count,
//                TotalAspirantes = aspirantes.Count,

//                PreinscripcionesPendientes = preinscripciones.Count(p => p.EstadoPreinscripcion == EstadoPreinscripcion.Pendiente),
//                PreinscripcionesConvertidas = preinscripciones.Count(p => p.EstadoPreinscripcion == EstadoPreinscripcion.Convertida),
//                PreinscripcionesCanceladas = preinscripciones.Count(p => p.EstadoPreinscripcion == EstadoPreinscripcion.Cancelada),

//                AspirantesPendientes = aspirantes.Count(a => a.EstadoRegistro == EstadoRegistro.Pendiente),
//                AspirantesAprobados = aspirantes.Count(a => a.EstadoRegistro == EstadoRegistro.Aprobado),
//                AspirantesRechazados = aspirantes.Count(a => a.EstadoRegistro == EstadoRegistro.Rechazado),

//                PreinscripcionesPorCarrera = preinscripciones
//                    .GroupBy(p => p.CarreraSolicitada)
//                    .ToDictionary(g => g.Key, g => g.Count()),
//                AspirantesPorCarrera = aspirantes
//                    .GroupBy(a => a.CarreraSolicitada)
//                    .ToDictionary(g => g.Key, g => g.Count()),

//                PreinscripcionesPorEstado = preinscripciones
//                    .Where(p => !string.IsNullOrEmpty(p.Estado))
//                    .GroupBy(p => p.Estado)
//                    .OrderByDescending(g => g.Count())
//                    .Take(10)
//                    .ToDictionary(g => g.Key, g => g.Count()),
//                AspirantesPorEstado = aspirantes
//                    .Where(a => !string.IsNullOrEmpty(a.Estado))
//                    .GroupBy(a => a.Estado)
//                    .OrderByDescending(g => g.Count())
//                    .Take(10)
//                    .ToDictionary(g => g.Key, g => g.Count()),

//                UltimasPreinscripciones = preinscripciones
//                    .OrderByDescending(p => p.FechaPreinscripcion)
//                    .Take(10)
//                    .ToList(),
//                UltimosAspirantes = aspirantes
//                    .OrderByDescending(a => a.FechaRegistro)
//                    .Take(10)
//                    .ToList()
//            };

//            return View(vm);
//        }
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Data;
using ControlEscolar.Models;

namespace ControlEscolar.Controllers
{
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

                PreinscripcionesPendientes = preinscripciones.Count(p => p.EstadoPreinscripcion == "Pendiente"),
                PreinscripcionesConvertidas = preinscripciones.Count(p => p.EstadoPreinscripcion == "Convertida"),
                PreinscripcionesCanceladas = preinscripciones.Count(p => p.EstadoPreinscripcion == "Cancelada"),

                AspirantesPendientes = aspirantes.Count(a => a.EstadoInscripcion == "Pendiente"),
                AspirantesAprobados = aspirantes.Count(a => a.EstadoInscripcion == "Aprobado"),
                AspirantesRechazados = aspirantes.Count(a => a.EstadoInscripcion == "Rechazado"),

                PreinscripcionesPorCarrera = preinscripciones
                    .GroupBy(p => p.CarreraSolicitada)
                    .ToDictionary(g => g.Key, g => g.Count()),

                AspirantesPorCarrera = aspirantes
                    .GroupBy(a => a.CarreraSolicitada)
                    .ToDictionary(g => g.Key, g => g.Count()),

                PreinscripcionesPorEstado = preinscripciones
                    .Where(p => !string.IsNullOrEmpty(p.Domicilio?.Estado))
                    .GroupBy(p => p.Domicilio!.Estado)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),

                AspirantesPorEstado = aspirantes
                    .Where(a => !string.IsNullOrEmpty(a.Preinscripcion?.Domicilio?.Estado))
                    .GroupBy(a => a.Preinscripcion!.Domicilio!.Estado)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),

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
    }
}
