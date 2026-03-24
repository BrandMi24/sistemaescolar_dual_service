using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class AsistenciaController : Controller
    {
        public IActionResult Index()
        {
            // Busca y muestra el archivo HTML en: Views/Asistencia/Index.cshtml
            return View();
        }
    }
}
