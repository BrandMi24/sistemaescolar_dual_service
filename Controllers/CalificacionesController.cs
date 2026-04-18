using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class CalificacionesController : Controller
    {
        // Esta acción se activa cuando entras a la sección de Calificaciones
        public IActionResult Index()
        {
            // Busca automáticamente el HTML en: Views/Calificaciones/Index.cshtml
            return View();
        }
    }
}
