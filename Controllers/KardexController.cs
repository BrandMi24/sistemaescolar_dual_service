using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Alumno,Student,Tutor,Teacher,Maestro,Coordinador,Admin,Administrator,Master")]
    public class KardexController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
