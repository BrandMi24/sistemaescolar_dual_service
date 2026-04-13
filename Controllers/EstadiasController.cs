using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Tutor,Teacher,Maestro,AsesorAcademico,Asesor,Coordinador,Admin,Administrator,Master")]
    public class EstadiasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
