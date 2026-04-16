using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ControlEscolar.Controllers
{
    public class TutorController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos la base de datos
        public TutorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Asistencia()
        {
            return View();
        }

        public IActionResult Entrevista()
        {
            return View();
        }

        public IActionResult Seguimiento()
        {
            return View();
        }

        // ====================================================
        // AQUÍ CARGAMOS LA BANDEJA DE TRÁMITES PARA EL TUTOR
        // ====================================================
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize(Roles = "Administrativo,TEACHER,ADMIN")]
        public IActionResult Tramites(string estatus = "Todos")
        {
            var listado = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_admin_get_solicitudes'")
                .AsEnumerable()
                .ToList();

            if (estatus != "Todos")
            {
                listado = listado.Where(x => x.Estatus == estatus).ToList();
            }

            ViewBag.EstatusActual = estatus;
            return View(listado);
        }
    }
}