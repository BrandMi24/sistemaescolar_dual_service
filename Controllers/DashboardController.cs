using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    // Solo los administradores deberían entrar aquí
    // [Authorize(Roles = "ADMIN")] 
    public class DashboardController : Controller
    {
        // 1. Visión Rectoría
        public IActionResult Index()
        {
            return View();
        }

        // 2. Aspirantes
        public IActionResult Aspirantes()
        {
            return View();
        }

        // 3. Admisiones
        public IActionResult Admisiones()
        {
            return View();
        }

        // 4. Trámites Operativos
        public IActionResult Tramites()
        {
            return View();
        }

        // 5. Diagnóstico de Base de Datos
        public IActionResult Diagnostico()
        {
            return View();
        }
    }
}