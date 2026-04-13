namespace ControlEscolar.ViewModels.StudentPortal;

public class StudentPortalDocumentRowViewModel
{
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string? FilePath { get; set; }
    public string? Notes { get; set; }
    public int? WeekNumber { get; set; }
    public decimal? HoursWorked { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public string? ReviewComments { get; set; }
    public DateTime? ReviewDate { get; set; }
}

public class StudentPortalOrganizationOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = "Sin direccion";
    public string ContactName { get; set; } = "Sin contacto";
    public string Email { get; set; } = "Sin correo";
    public string Phone { get; set; } = "Sin telefono";
}

public class DualPortalViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "S/N";
    public string Curp { get; set; } = "N/D";
    public string CareerName { get; set; } = "Sin carrera";
    public string GroupCode { get; set; } = "Sin grupo";
    public string Shift { get; set; } = "Sin turno";
    public string Email { get; set; } = "Sin correo";
    public string Phone { get; set; } = "Sin telefono";
    public string Grade { get; set; } = string.Empty;
    public string InternshipPeriod { get; set; } = string.Empty;
    public int? InternshipYear { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentStatusCode { get; set; } = "REGISTERED";
    public string OrganizationName { get; set; } = "Sin empresa";
    public string OrganizationContact { get; set; } = "Sin contacto";
    public string AcademicAdvisorName { get; set; } = "Sin asignar";
    public string AcademicAdvisorEmail { get; set; } = "Sin correo";
    public string AcademicAdvisorPhone { get; set; } = "Sin telefono";
    public string BusinessAdvisorName { get; set; } = "Sin registrar";
    public string BusinessAdvisorRole { get; set; } = "N/D";
    public string BusinessAdvisorEmail { get; set; } = "N/D";
    public decimal ApprovedHours { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal ProgressPercent { get; set; }
    public int RequestedLettersCount { get; set; }
    public bool HasResumeSpanish { get; set; }
    public bool HasResumeEnglish { get; set; }
    public bool HasImssCertificate { get; set; }
    public bool HasAcceptanceLetter { get; set; }
    public int OrganizationId { get; set; }
    public List<StudentPortalOrganizationOptionViewModel> AvailableOrganizations { get; set; } = new();
    public List<StudentPortalDocumentRowViewModel> WeeklyReports { get; set; } = new();
    public List<StudentPortalDocumentRowViewModel> Documents { get; set; } = new();
}

public class SocialServicePortalViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "S/N";
    public string Curp { get; set; } = "N/D";
    public string CareerName { get; set; } = "Sin carrera";
    public string GroupCode { get; set; } = "Sin grupo";
    public string Shift { get; set; } = "Sin turno";
    public string Email { get; set; } = "Sin correo";
    public string Phone { get; set; } = "Sin telefono";
    public string Grade { get; set; } = string.Empty;
    public string ServicePeriod { get; set; } = string.Empty;
    public int? ServiceYear { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentStatusCode { get; set; } = "REGISTERED";
    public string OrganizationName { get; set; } = "Sin institucion";
    public string OrganizationContact { get; set; } = "Sin contacto";
    public string AcademicAdvisorName { get; set; } = "Sin asignar";
    public string AcademicAdvisorEmail { get; set; } = "Sin correo";
    public string AcademicAdvisorPhone { get; set; } = "Sin telefono";
    public decimal TotalHours { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal? EvaluationScore { get; set; }
    public string? EvaluationNotes { get; set; }
    public bool IsReleased { get; set; }
    public int RequestedLettersCount { get; set; }
    public bool HasAcceptanceLetter { get; set; }
    public int OrganizationId { get; set; }
    public List<StudentPortalOrganizationOptionViewModel> AvailableOrganizations { get; set; } = new();
    public List<StudentPortalDocumentRowViewModel> WeeklyReports { get; set; } = new();
    public List<StudentPortalDocumentRowViewModel> HourLogs { get; set; } = new();
    public List<StudentPortalDocumentRowViewModel> Documents { get; set; } = new();
}
