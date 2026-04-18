using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    // [Authorize] // Descomenta esto si ya tienes implementado el login de usuarios
    public class EntrevistaController : Controller
    {
        // Este método carga la vista principal de la Entrevista y Asignación
        public IActionResult Index()
        {
            return View();
        }
    }
}