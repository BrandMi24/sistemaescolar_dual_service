namespace ControlEscolar.ViewModels.DualEducation;

public class PlacementSupportViewModel
{
    public string PlacementType { get; set; } = "NEEDS_SUPPORT";
    public List<int> SelectedParkIds { get; set; } = new();
    public int? SelectedOrganizationId { get; set; }
    public string? CompanyLegalName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? HRContactName { get; set; }
    public string? HRContactEmail { get; set; }
    public string? HRContactPhone { get; set; }
    public string? SupervisorRole { get; set; }
}
