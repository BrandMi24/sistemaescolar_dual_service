using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    public class KardexController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
