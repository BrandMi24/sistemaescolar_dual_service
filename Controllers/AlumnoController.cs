using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authorization;
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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize(Roles = "STUDENT, ADMIN")]
        public IActionResult Tramites()
        {
            ViewData["Title"] = "Trámites Escolares";
            ViewData["ShowBackButton"] = true;

            // Buscamos el ID de forma segura con la lógica "todoterreno"
            var userIdClaim = User.FindFirst("UserId")?.Value
                              ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            int userIdActual = int.TryParse(userIdClaim, out int id) ? id : 0;

            if (userIdActual == 0) return View(new List<DetalleSolicitudViewModel>());

            var historial = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_getbyalumno', @ID={userIdActual}")
                .AsEnumerable()
                .ToList();

            return View(historial);
        }
    }
}