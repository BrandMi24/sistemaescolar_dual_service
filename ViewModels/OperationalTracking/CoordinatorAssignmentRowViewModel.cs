namespace ControlEscolar.ViewModels.OperationalTracking;

public class CoordinatorAssignmentRowViewModel
{
    public int AssignmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? Matricula { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = "Sin organizacion";
    public int? TeacherId { get; set; }
    public string TeacherName { get; set; } = "SIN ASIGNAR";
    public string StatusCode { get; set; } = string.Empty;
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
    public int ProgressPercent => RequiredHours > 0 ? (int)Math.Min(100, Math.Round((ApprovedHours / RequiredHours) * 100m)) : 0;
}
