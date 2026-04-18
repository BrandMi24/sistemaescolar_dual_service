using System.Collections.Generic;

namespace ControlEscolar.Models
{
    public class UsuariosViewModel
    {
        public List<UserDetalle> Users { get; set; } = new List<UserDetalle>();
    }

    public class UserDetalle
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rol { get; set; } = "";
    }

    public class AsignarRolRequest
    {
        public int UserId { get; set; }
        public int RolId { get; set; }
    }

    public class QuitarRolRequest
    {
        public int UserId { get; set; }
    }
}