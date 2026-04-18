using ControlEscolar.Models.Dashboard;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Admin, Rectoria, Academico")]
    public class DashAcademicController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashAcademicController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetAcademicQualityDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new AcademicQualityViewModel());
            }
        }
    }
}