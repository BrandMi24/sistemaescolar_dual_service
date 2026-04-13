using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_person_table")]
public class Person
{
    [Key]
    [Column("management_person_ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("management_person_FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("management_person_LastNamePaternal")]
    public string LastNamePaternal { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("management_person_LastNameMaternal")]
    public string? LastNameMaternal { get; set; }

    [StringLength(18)]
    [Column("management_person_CURP")]
    public string? CURP { get; set; }

    [StringLength(150)]
    [Column("management_person_Email")]
    public string? Email { get; set; }

    [StringLength(30)]
    [Column("management_person_Phone")]
    public string? Phone { get; set; }

    [Column("management_person_status")]
    public bool Status { get; set; }

    [NotMapped]
    public string FullName => $"{FirstName} {LastNamePaternal} {LastNameMaternal}".Trim();
}
