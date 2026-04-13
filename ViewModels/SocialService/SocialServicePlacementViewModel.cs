namespace ControlEscolar.ViewModels.SocialService;

public class SocialServicePlacementViewModel
{
    public string PlacementType { get; set; } = "NEEDS_HELP";
    public int? SelectedZoneId { get; set; }
    public int? SelectedInstitutionId { get; set; }
    public int? SelectedOrganizationId { get; set; }
    public string? InstitutionName { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
