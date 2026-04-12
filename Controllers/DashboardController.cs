using Microsoft.AspNetCore.Mvc;
using ControlEscolar.Services; // Asegúrate de que apunte a donde está IDashboardService
using ControlEscolar.Models.Dashboard;

namespace ControlEscolar.Controllers
{
    // [Authorize] // Agrégalo cuando quieras activar la seguridad
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // 1. Inicio (Visión Rectoría)
        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetRectorateDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 2. Inscripciones
        public async Task<IActionResult> Admisiones(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAdmissionsDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 3. Trámites
        public async Task<IActionResult> Tramites(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetTramitesDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 4. Vinculación
        public async Task<IActionResult> Vinculacion(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetVinculacionDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 5. Calidad Académica
        public async Task<IActionResult> Calidad(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAcademicQualityDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 6. Servicios Médicos
        public async Task<IActionResult> Salud(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetMedicalDataAsync(year, cuatrimestre);
            return View(model);
        }
    }
}