using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class EstadiasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
