using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class CoordinadorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // 2. Organización Escolar
        public IActionResult Ciclos()
        {
            return View();
        }

        public IActionResult Catalogos()
        {
            return View();
        }

        // 3. Gestión Operativa
        public IActionResult Asignaciones()
        {
            return View();
        }

        public IActionResult Grupos()
        {
            return View();
        }

        // 4. Analítica
        public IActionResult Reportes()
        {
            return View();
        }
    }
}
