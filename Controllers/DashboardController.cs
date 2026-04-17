using Microsoft.AspNetCore.Mvc;
using ControlEscolar.Services;
using ControlEscolar.Models.Dashboard;

namespace ControlEscolar.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetRectorateDataAsync(year, cuatrimestre);
            return View(model);
        }

        public async Task<IActionResult> Admisiones(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAdmissionsDataAsync(year, cuatrimestre);
            return View(model);
        }

        public async Task<IActionResult> Tramites(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetTramitesDataAsync(year, cuatrimestre);
            return View(model);
        }

        public async Task<IActionResult> Vinculacion(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetVinculacionDataAsync(year, cuatrimestre);
            return View(model);
        }

        public async Task<IActionResult> Calidad(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAcademicQualityDataAsync(year, cuatrimestre);
            return View(model);
        }

        public async Task<IActionResult> Salud(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetMedicalDataAsync(year, cuatrimestre);
            return View(model);
        }
    }
}