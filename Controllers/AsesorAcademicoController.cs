using ControlEscolar.Data;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ControlEscolar.Controllers
{
    public class AsesorAcademicoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AsesorAcademicoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Vista: Mis Estudiantes
        public IActionResult AlumnosAsignados()
        {
            ViewData["Title"] = "Mis Estudiantes Asignados";
            return View();
        }

        // Vista: Revisión de Docs
        public IActionResult RevisionDocumentos()
        {
            ViewData["Title"] = "Validación de Documentación";
            return View();
        }

        // Vista: Estado de Evaluación
        public IActionResult Evaluaciones()
        {
            ViewData["Title"] = "Evaluaciones Académicas";
            return View();
        }

        // Acción opcional para el botón "Volver al Dashboard"
        public IActionResult Index()
        {
            return RedirectToAction("AlumnosAsignados");
        }
    }
}