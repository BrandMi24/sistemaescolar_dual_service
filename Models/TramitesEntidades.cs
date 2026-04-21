using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models
{
    [Table("CE_TramitesCategoria", Schema = "dbo")]
    public class Cat_Tramites
    {
        [Key]
        public int id_tramite { get; set; }
        public string nombre_tramite { get; set; } = string.Empty;
        public bool activo { get; set; } = true;
    }

    [Table("CE_TramitesRequisitos", Schema = "dbo")]
    public class Requisito_Tramite
    {
        [Key]
        public int id_requisito { get; set; }
        public int id_tramite { get; set; }
        public string nombre_documento { get; set; } = "";

        [ForeignKey("id_tramite")]
        public virtual Cat_Tramites Categoria { get; set; } = null!;
    }

    [Table("CE_TramitesSolicitud", Schema = "dbo")]
    public class Solicitud
    {
        [Key]
        public int tramites_solicitud_id { get; set; }

        [Required]
        public int id_usuario_propietario { get; set; }

        public DateTime tramites_solicitud_fecha { get; set; } = DateTime.Now;

        [Required]
        public int id_tramite { get; set; }

        public string? tramites_solicitud_estatus { get; set; } = "Pendiente";
        public string? tramites_solicitud_archivo_path { get; set; } = "";
        public string? tramites_solicitud_observaciones { get; set; }

        [NotMapped]
        public int grado_solicitud { get; set; }

        [NotMapped]
        public string grupo_solicitud { get; set; } = string.Empty;

        [ForeignKey("id_tramite")]
        public virtual Cat_Tramites Categoria { get; set; } = null!;
    }

    [Table("CE_TramitesDetalleDocumentos", Schema = "dbo")]
    public class DetalleDocumentos
    {
        [Key]
        public int id_detalle_doc { get; set; }
        public int id_solicitud { get; set; }
        public int id_requisito { get; set; }

        // Agregamos el '?' a los strings que pueden venir nulos de la BD
        public string? estatus_documento { get; set; } = "Pendiente";
        public string? motivo_rechazo { get; set; }
        public DateTime? fecha_validacion { get; set; }
        public string? nombre_archivo_fisico { get; set; }

        [ForeignKey("id_solicitud")]
        public virtual Solicitud? Solicitud { get; set; }

        [ForeignKey("id_requisito")]
        public virtual Requisito_Tramite? Requisito { get; set; }
    }

    [Table("management_user_table")]
    public class ManagementUser
    {
        [Key]
        public int management_user_ID { get; set; }
        public int? management_user_PersonID { get; set; }
        [Required]
        public string? management_user_Username { get; set; }
        public string? management_user_Email { get; set; }
        [Required]
        public string? management_user_PasswordHash { get; set; }
        public bool management_user_IsLocked { get; set; }
        public string? management_user_LockReason { get; set; }
        public DateTime? management_user_LastLoginDate { get; set; }
        public bool management_user_status { get; set; }
        public DateTime management_user_createdDate { get; set; }
        public int? management_user_RoleID { get; set; }
    }
}