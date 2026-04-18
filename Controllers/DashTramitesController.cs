using ControlEscolar.Models.Dashboard;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Admin, Rectoria, Servicios Escolares")]
    public class DashTramitesController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashTramitesController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetTramitesDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new TramitesViewModel());
            }
        }
    }
}