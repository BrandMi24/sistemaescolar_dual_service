using Microsoft.AspNetCore.Http;

namespace ControlEscolar.ViewModels.DualEducation;

public class WeeklyReportsViewModel
{
    public int WeekNumber { get; set; }
    public string ReportTitle { get; set; } = string.Empty;
    public decimal HoursWorked { get; set; }
    public IFormFile? ReportFile { get; set; }
}
