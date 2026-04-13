using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Tutor,Teacher,Maestro,Coordinador,Admin,Administrator,Master")]
    public class AsistenciaController : Controller
    {
        public IActionResult Index()
        {
            // Busca y muestra el archivo HTML en: Views/Asistencia/Index.cshtml
            return View();
        }
    }
}
