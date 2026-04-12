using ControlEscolar.Models.Dashboard;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Admin, Rectoria")]
    public class RectoriaController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public RectoriaController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetRectorateDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new RectorateViewModel());
            }
        }
    }
}