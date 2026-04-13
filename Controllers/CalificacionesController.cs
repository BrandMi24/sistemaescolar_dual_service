using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Tutor,Teacher,Maestro,Coordinador,Admin,Administrator,Master")]
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
