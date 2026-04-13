namespace ControlEscolar.ViewModels.OperationalTracking;

public class AsesorAlumnoRowViewModel
{
    public int AssignmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string Matricula { get; set; } = "N/A";
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = "Sin organización";
    public string StatusCode { get; set; } = string.Empty;
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
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
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public decimal ApprovedHours { get; set; }
    public int RequiredHours { get; set; }
    public int ProgressPercent => RequiredHours > 0
        ? (int)Math.Min(100, Math.Round((ApprovedHours / RequiredHours) * 100m))
        : 0;
    public decimal? EvaluationScore { get; set; }
    public string? EvaluationNotes { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public bool IsEvaluated => EvaluationScore.HasValue;
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
