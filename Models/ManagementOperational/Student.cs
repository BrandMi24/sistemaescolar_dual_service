using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_student_table")]
public class Student
{
    [Key]
    [Column("management_student_ID")]
    public int Id { get; set; }

    [Column("management_student_PersonID")]
    public int PersonId { get; set; }

    [Column("management_student_CareerID")]
    public int? CareerId { get; set; }

    [Column("management_student_GroupID")]
    public int? GroupId { get; set; }

    [Column("management_student_Matricula")]
    public string? Matricula { get; set; }

    [Column("management_student_StatusCode")]
    public string StatusCode { get; set; } = "INSCRITO";

    [Column("management_student_status")]
    public bool Status { get; set; }

    [ForeignKey(nameof(PersonId))]
    public Person Person { get; set; } = null!;

    [ForeignKey(nameof(CareerId))]
    public Career? Career { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }
}
