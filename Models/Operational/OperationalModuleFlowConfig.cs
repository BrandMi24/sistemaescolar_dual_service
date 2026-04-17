using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.Operational;

[Table("operational_moduleflowconfig_table")]
public class OperationalModuleFlowConfig
{
    [Key]
    [Column("operational_moduleflowconfig_ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Column("operational_moduleflowconfig_ModuleType")]
    public string ModuleType { get; set; } = string.Empty;

    [Column("operational_moduleflowconfig_PortalStartCuatrimestre")]
    public int PortalStartCuatrimestre { get; set; } = 10;

    [Column("operational_moduleflowconfig_TrackingStartCuatrimestre")]
    public int TrackingStartCuatrimestre { get; set; } = 11;

    [Column("operational_moduleflowconfig_status")]
    public bool Status { get; set; } = true;

    [Column("operational_moduleflowconfig_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("operational_moduleflowconfig_updatedDate")]
    public DateTime? UpdatedDate { get; set; }
}
