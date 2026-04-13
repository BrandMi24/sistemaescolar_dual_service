using Microsoft.AspNetCore.Http;

namespace ControlEscolar.ViewModels.SocialService;

public class SocialServiceWeeklyReportViewModel
{
    public int WeekNumber { get; set; }
    public string ReportTitle { get; set; } = string.Empty;
    public decimal HoursWorked { get; set; }
    public IFormFile? ReportFile { get; set; }
}