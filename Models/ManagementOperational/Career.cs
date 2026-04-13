using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models.ManagementOperational;

[Table("management_career_table")]
public class Career
{
    [Key]
    [Column("management_career_ID")]
    public int Id { get; set; }

    [StringLength(150)]
    [Column("management_career_Name")]
    public string Name { get; set; } = string.Empty;
}
