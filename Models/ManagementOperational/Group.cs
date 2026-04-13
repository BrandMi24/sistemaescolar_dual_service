using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_group_table")]
public class Group
{
    [Key]
    [Column("management_group_ID")]
    public int Id { get; set; }

    [StringLength(30)]
    [Column("management_group_Code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("management_group_Shift")]
    public string? Shift { get; set; }
}
