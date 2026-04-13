namespace ControlEscolar.ViewModels.SocialService;

public class SocialServiceHoursLogViewModel
{
    public DateTime LogDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string ActivityDescription { get; set; } = string.Empty;
}
