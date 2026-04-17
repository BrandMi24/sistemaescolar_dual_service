using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.Operational;

[Table("operational_modulesteprule_table")]
public class OperationalModuleStepRule
{
    [Key]
    [Column("operational_modulesteprule_ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Column("operational_modulesteprule_ModuleType")]
    public string ModuleType { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Column("operational_modulesteprule_StepCode")]
    public string StepCode { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [Column("operational_modulesteprule_StepName")]
    public string StepName { get; set; } = string.Empty;

    [Column("operational_modulesteprule_MinCuatrimestre")]
    public int? MinCuatrimestre { get; set; }

    [StringLength(400)]
    [Column("operational_modulesteprule_AllowedStatusesCsv")]
    public string? AllowedStatusesCsv { get; set; }

    [Column("operational_modulesteprule_SortOrder")]
    public int SortOrder { get; set; }

    [Column("operational_modulesteprule_status")]
    public bool Status { get; set; } = true;

    [Column("operational_modulesteprule_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("operational_modulesteprule_updatedDate")]
    public DateTime? UpdatedDate { get; set; }
}
