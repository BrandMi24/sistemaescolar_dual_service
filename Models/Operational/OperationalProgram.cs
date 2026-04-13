using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControlEscolar.Models.ManagementOperational;

namespace ControlEscolar.Models.Operational;

[Table("operational_program_table")]
public class OperationalProgram
{
    [Key]
    [Column("operational_program_ID")]
    public int Id { get; set; }

    [Column("operational_program_Code")]
    public string Code { get; set; } = string.Empty;

    [Column("operational_program_Name")]
    public string Name { get; set; } = string.Empty;

    [Column("operational_program_Type")]
    public string Type { get; set; } = string.Empty;

    [Column("operational_program_Period")]
    public string? Period { get; set; }

    [Column("operational_program_Year")]
    public int? Year { get; set; }

    [Column("operational_program_CareerID")]
    public int? CareerId { get; set; }

    [Column("operational_program_CoordinatorID")]
    public int? CoordinatorId { get; set; }

    [Column("operational_program_RequiredHours")]
    public int RequiredHours { get; set; } = 480;

    [Column("operational_program_IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("operational_program_status")]
    public bool Status { get; set; } = true;

    [ForeignKey(nameof(CareerId))]
    public Career? Career { get; set; }

    [ForeignKey(nameof(CoordinatorId))]
    public Teacher? Coordinator { get; set; }

    public ICollection<OperationalStudentAssignment> StudentAssignments { get; set; } = new List<OperationalStudentAssignment>();
}
