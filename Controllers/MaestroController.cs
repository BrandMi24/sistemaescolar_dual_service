using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class MaestroController : Controller
    {
        public IActionResult Index() => View();

        // Asegúrate que diga "Materias" con M mayúscula y en plural
        public IActionResult Materias()
        {
            return View();
        }

        // Asegúrate que diga "Grupos" con G mayúscula
        public IActionResult Grupos()
        {
            return View();
        }

        public IActionResult Asistencia() => View();
    }
}