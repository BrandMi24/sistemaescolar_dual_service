using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControlEscolar.Models.ManagementOperational;

namespace ControlEscolar.Models.Operational;

[Table("operational_studentassignment_table")]
public class OperationalStudentAssignment
{
    [Key]
    [Column("operational_studentassignment_ID")]
    public int Id { get; set; }

    [Column("operational_studentassignment_StudentID")]
    public int StudentId { get; set; }

    [Column("operational_studentassignment_ProgramID")]
    public int ProgramId { get; set; }

    [Column("operational_studentassignment_OrganizationID")]
    public int? OrganizationId { get; set; }

    [Column("operational_studentassignment_TeacherID")]
    public int? TeacherId { get; set; }

    [Column("operational_studentassignment_StatusCode")]
    public string StatusCode { get; set; } = "REGISTERED";

    [Column("operational_studentassignment_TotalHours", TypeName = "decimal(8,2)")]
    public decimal TotalHours { get; set; }

    [Column("operational_studentassignment_ApprovedHours", TypeName = "decimal(8,2)")]
    public decimal ApprovedHours { get; set; }

    [Column("operational_studentassignment_EvaluationScore", TypeName = "decimal(5,2)")]
    public decimal? EvaluationScore { get; set; }

    [Column("operational_studentassignment_EvaluationNotes")]
    public string? EvaluationNotes { get; set; }

    [Column("operational_studentassignment_status")]
    public bool Status { get; set; } = true;

    [Column("operational_studentassignment_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    [ForeignKey(nameof(ProgramId))]
    public OperationalProgram Program { get; set; } = null!;

    [ForeignKey(nameof(OrganizationId))]
    public OperationalOrganization? Organization { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }

    public ICollection<OperationalDocument> Documents { get; set; } = new List<OperationalDocument>();
}
