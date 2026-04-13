namespace ControlEscolar.Models;

public class CoordinadorStudentDetailViewModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "S/N";
    public string Email { get; set; } = "Sin correo";
    public string Phone { get; set; } = "Sin telefono";
    public string StatusCode { get; set; } = "N/A";
    public bool IsActive { get; set; }
    public string Curp { get; set; } = "N/D";
    public string CareerName { get; set; } = "Sin carrera";
    public string GroupName { get; set; } = "Sin grupo";
    public decimal ApprovedHours { get; set; }
    public decimal PendingHours { get; set; }
    public int DocumentsCount { get; set; }
    public int EvaluationsCount { get; set; }
    public List<CoordinadorStudentAssignmentRowViewModel> Assignments { get; set; } = new();
    public List<CoordinadorStudentDocumentRowViewModel> Documents { get; set; } = new();
}

public class CoordinadorStudentAssignmentRowViewModel
{
    public int AssignmentId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string TeacherName { get; set; } = "Sin asignar";
    public string StatusCode { get; set; } = "N/A";
    public decimal ApprovedHours { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal? EvaluationScore { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CoordinadorStudentDocumentRowViewModel
{
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}

public class CoordinadorTeacherDetailViewModel
{
    public int TeacherId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = "N/D";
    public string Email { get; set; } = "Sin correo";
    public string Phone { get; set; } = "Sin telefono";
    public string StatusCode { get; set; } = "N/A";
    public bool IsActive { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public decimal PendingHours { get; set; }
    public int PendingDocuments { get; set; }
    public int EvaluationsCount { get; set; }
    public decimal CompletionRate { get; set; }
    public List<CoordinadorTeacherStudentRowViewModel> AssignedStudents { get; set; } = new();
}

public class CoordinadorTeacherStudentRowViewModel
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Matricula { get; set; } = "S/N";
    public string CareerName { get; set; } = "Sin carrera";
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramType { get; set; } = string.Empty;
    public string StatusCode { get; set; } = "N/A";
    public decimal ApprovedHours { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal ProgressPercent { get; set; }
}