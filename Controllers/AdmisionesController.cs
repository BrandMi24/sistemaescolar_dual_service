using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    // Este controlador será exclusivo para la nueva bandeja visual
    public class AdmisionesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}