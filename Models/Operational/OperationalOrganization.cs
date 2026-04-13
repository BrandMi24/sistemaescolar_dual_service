using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.Operational;

[Table("operational_organization_table")]
public class OperationalOrganization
{
    [Key]
    [Column("operational_organization_ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Column("operational_organization_Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("operational_organization_Type")]
    public string Type { get; set; } = string.Empty;

    [StringLength(300)]
    [Column("operational_organization_Address")]
    public string? Address { get; set; }

    [StringLength(30)]
    [Column("operational_organization_Phone")]
    public string? Phone { get; set; }

    [StringLength(150)]
    [Column("operational_organization_Email")]
    public string? Email { get; set; }

    [StringLength(150)]
    [Column("operational_organization_ContactName")]
    public string? ContactName { get; set; }

    [Column("operational_organization_status")]
    public bool Status { get; set; } = true;

    [Column("operational_organization_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public ICollection<OperationalStudentAssignment> StudentAssignments { get; set; } = new List<OperationalStudentAssignment>();
}
