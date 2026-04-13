using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    // Este controlador será exclusivo para la nueva bandeja visual
    [Authorize(Roles = "Admisiones,Preinscripciones,Administrativo,Coordinador,Director,Admin,Administrator,Master")]
    public class AdmisionesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}