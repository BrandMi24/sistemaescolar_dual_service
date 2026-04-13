using ControlEscolar.Models.Operational;

namespace ControlEscolar.ViewModels.OperationalTracking;

public class TutorSeguimientoViewModel
{
    public int? AssignmentId { get; set; }
    public int? StudentId { get; set; }
    public string StudentName { get; set; } = "Sin alumno asignado";
    public string Matricula { get; set; } = "N/A";
    public string CareerCode { get; set; } = "N/A";
    public string GroupCode { get; set; } = "N/A";
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; } = 480;
    public string StatusCode { get; set; } = "SIN_DATOS";
    public List<OperationalDocument> TimelineDocuments { get; set; } = new();
}
