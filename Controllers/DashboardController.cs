using Microsoft.AspNetCore.Mvc;
using ControlEscolar.Services;
using ControlEscolar.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;

namespace ControlEscolar.Controllers
{
    [Authorize] // Requiere login para cualquier acción de este controller
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // 1. Inicio (Visión Rectoría) — solo dirección
        [Authorize(Roles = "ADMIN,Coordinador")]
        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetRectorateDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 2. Admisiones
        [Authorize(Roles = "ADMIN,Coordinador,Administrativo")]
        public async Task<IActionResult> Admisiones(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAdmissionsDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 3. Trámites
        [Authorize(Roles = "ADMIN,Coordinador,Administrativo")]
        public async Task<IActionResult> Tramites(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetTramitesDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 4. Vinculación — incluye Administrativo de Vinculación
        [Authorize(Roles = "ADMIN,Coordinador,Administrativo,Administrativo de Vinculación")]
        public async Task<IActionResult> Vinculacion(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetVinculacionDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 5. Calidad Académica — incluye TEACHER
        [Authorize(Roles = "ADMIN,Coordinador,Administrativo,TEACHER")]
        public async Task<IActionResult> Calidad(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetAcademicQualityDataAsync(year, cuatrimestre);
            return View(model);
        }

        // 6. Salud Integral — personal médico y psicológico
        [Authorize(Roles = "ADMIN,Coordinador,Head Nurse,Nurse,Physicologyst")]
        public async Task<IActionResult> Salud(int? year, int? cuatrimestre)
        {
            var model = await _dashboardService.GetMedicalDataAsync(year, cuatrimestre);
            return View(model);
        }
    }
}