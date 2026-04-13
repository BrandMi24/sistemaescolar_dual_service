using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControlEscolar.Models.ManagementOperational;

namespace ControlEscolar.Models.Operational;

[Table("operational_document_table")]
public class OperationalDocument
{
    [Key]
    [Column("operational_document_ID")]
    public int Id { get; set; }

    [Column("operational_document_AssignmentID")]
    public int AssignmentId { get; set; }

    [Column("operational_document_UploadedByUserID")]
    public int? UploadedByUserId { get; set; }

    [Column("operational_document_DocumentType")]
    public string DocumentType { get; set; } = string.Empty;

    [Column("operational_document_Title")]
    public string Title { get; set; } = string.Empty;

    [Column("operational_document_FilePath")]
    public string? FilePath { get; set; }

    [Column("operational_document_OriginalFileName")]
    public string? OriginalFileName { get; set; }

    [Column("operational_document_ContentType")]
    public string? ContentType { get; set; }

    [Column("operational_document_FileSize")]
    public long? FileSize { get; set; }

    [Column("operational_document_Notes")]
    public string? Notes { get; set; }

    [Column("operational_document_StatusCode")]
    public string StatusCode { get; set; } = "PENDING";

    [Column("operational_document_ReviewedByTeacherID")]
    public int? ReviewedByTeacherId { get; set; }

    [Column("operational_document_ReviewDate")]
    public DateTime? ReviewDate { get; set; }

    [Column("operational_document_ReviewComments")]
    public string? ReviewComments { get; set; }

    [Column("operational_document_UploadDate")]
    public DateTime UploadDate { get; set; } = DateTime.Now;

    [Column("operational_document_status")]
    public bool Status { get; set; } = true;

    [Column("operational_document_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey(nameof(AssignmentId))]
    public OperationalStudentAssignment Assignment { get; set; } = null!;

    [ForeignKey(nameof(ReviewedByTeacherId))]
    public Teacher? ReviewedByTeacher { get; set; }
}
