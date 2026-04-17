using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_cuatrimestre_table")]
public class CuatrimestreCatalog
{
    [Key]
    [Column("management_cuatrimestre_ID")]
    public int Id { get; set; }

    [Column("management_cuatrimestre_Number")]
    public int Number { get; set; }

    [Required]
    [StringLength(80)]
    [Column("management_cuatrimestre_Name")]
    public string Name { get; set; } = string.Empty;

    [Column("management_cuatrimestre_IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("management_cuatrimestre_status")]
    public bool Status { get; set; } = true;

    [Column("management_cuatrimestre_createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("management_cuatrimestre_updatedDate")]
    public DateTime? UpdatedDate { get; set; }
}
