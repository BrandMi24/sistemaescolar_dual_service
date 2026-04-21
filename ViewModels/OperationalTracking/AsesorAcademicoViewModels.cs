namespace ControlEscolar.ViewModels.OperationalTracking;

public class AsesorAlumnoRowViewModel
{
    public int AssignmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string Matricula { get; set; } = "N/A";
    public string AdvisorName { get; set; } = "Sin asesor";
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = "Sin organización";
    public string StatusCode { get; set; } = string.Empty;
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
    public bool IsEvaluated { get; set; }
    public DateTime CreatedDate { get; set; }
    public int ProgressPercent => RequiredHours > 0
        ? (int)Math.Min(100, Math.Round((ApprovedHours / RequiredHours) * 100m))
        : 0;
}

public class AsesorDocumentoRowViewModel
{
    public int DocumentId { get; set; }
    public int AssignmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "N/A";
    public string AdvisorName { get; set; } = "Sin asesor";
    public string ProgramType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DateTime UploadDate { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string? ReviewComments { get; set; }
}

public class AsesorEvaluacionRowViewModel
{
    public int AssignmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string Matricula { get; set; } = "N/A";
    public string AdvisorName { get; set; } = "Sin asesor";
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
    public int ProgressPercent => RequiredHours > 0
        ? (int)Math.Min(100, Math.Round((ApprovedHours / RequiredHours) * 100m))
        : 0;
    public decimal? EvaluationScore { get; set; }
    public string? EvaluationNotes { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int ApprovedReports { get; set; }
    public bool IsEvaluated => EvaluationScore.HasValue;
    public bool HasPendingReports => PendingReports > 0;
    public bool ReadyForEvaluation => !IsEvaluated && !HasPendingReports && TotalReports > 0 && ProgressPercent >= 80;
}

public class AsesorEvaluacionesPageViewModel
{
    public int TotalStudents { get; set; }
    public int ReadyToEvaluate { get; set; }
    public int Evaluated { get; set; }
    public string AverageScore { get; set; } = "N/A";
    public string StatusFilter { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public List<AsesorEvaluacionRowViewModel> Rows { get; set; } = new();
}

// ============================================================
// DASHBOARD Y SEGUIMIENTO
// ============================================================

public class AsesorDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int ReadyToEvaluate { get; set; }
    public int Evaluated { get; set; }
    public decimal AverageHours { get; set; }
    public int PendingDocuments { get; set; }
    public List<AsesorAlumnoRowViewModel> Students { get; set; } = new();
    public List<AsesorAlumnoRowViewModel> RecentStudents { get; set; } = new();
}

public class AsesorTimelineItemViewModel
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string? Notes { get; set; }
    public string? FilePath { get; set; }
    public string? ReviewComments { get; set; }
    public string StatusLabel => StatusCode switch
    {
        "APPROVED" => "Aprobado",
        "REJECTED" => "Rechazado",
        "PENDING" => "Pendiente de Revisión",
        _ => StatusCode
    };
    public string StatusBadgeClass => StatusCode switch
    {
        "APPROVED" => "bg-success",
        "REJECTED" => "bg-danger",
        "PENDING" => "bg-warning text-dark",
        _ => "bg-secondary"
    };
}

public class AsesorSeguimientoViewModel
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "N/A";
    public string AdvisorName { get; set; } = "Sin asesor";
    public string CareerCode { get; set; } = "N/A";
    public string GroupCode { get; set; } = "N/A";
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = "Sin organización";
    public decimal TotalHours { get; set; }
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public int ProgressPercent => RequiredHours > 0
        ? (int)Math.Min(100, Math.Round((ApprovedHours / RequiredHours) * 100m))
        : 0;
    public decimal? EvaluationScore { get; set; }
    public string? EvaluationNotes { get; set; }
    public List<AsesorTimelineItemViewModel> Timeline { get; set; } = new();
}
