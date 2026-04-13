using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ControlEscolar.Controllers
{
    public class AlumnoController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos la base de datos
        public AlumnoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Inicio";
            ViewData["ShowBackButton"] = false;
            return View();
        }

        public IActionResult Entrevista()
        {
            ViewData["Title"] = "Entrevista Inicial";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult Calificaciones()
        {
            ViewData["Title"] = "Kardex";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult Asistencias()
        {
            ViewData["Title"] = "Mis Asistencias";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult ModeloDual()
        {
            ViewData["Title"] = "Modelo DUAL";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult ServicioSocial()
        {
            ViewData["Title"] = "Servicio Social";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        // ====================================================
        // AQUÍ CARGAMOS EL HISTORIAL DEL ALUMNO
        // ====================================================
        public IActionResult Tramites()
        {
            ViewData["Title"] = "Trámites Escolares";
            ViewData["ShowBackButton"] = true;

            // TODO: Por ahora usamos el ID '1' fijo para no tener errores.
            // Cuando tu login esté al 100%, aquí pondremos el ID del usuario real.
            int userIdActual = 1;

            var historial = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_getbyalumno', @ID={userIdActual}")
                .AsEnumerable()
                .ToList();

            return View(historial);
        }
    }
}