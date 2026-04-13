using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Tutor,Teacher,Maestro,AsesorAcademico,Asesor,Coordinador,Director,Admin,Administrator,Master")]
    public class EntrevistaController : Controller
    {
        // Este método carga la vista principal de la Entrevista y Asignación
        public IActionResult Index()
        {
            return View();
        }
    }
}