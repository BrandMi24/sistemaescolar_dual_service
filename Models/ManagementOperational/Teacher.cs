using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_teacher_table")]
public class Teacher
{
    [Key]
    [Column("management_teacher_ID")]
    public int Id { get; set; }

    [Column("management_teacher_PersonID")]
    public int PersonId { get; set; }

    [Column("management_teacher_EmployeeNumber")]
    public string? EmployeeNumber { get; set; }

    [Column("management_teacher_StatusCode")]
    public string StatusCode { get; set; } = "ACTIVO";

    [Column("management_teacher_status")]
    public bool Status { get; set; }

    [ForeignKey(nameof(PersonId))]
    public Person Person { get; set; } = null!;
}
