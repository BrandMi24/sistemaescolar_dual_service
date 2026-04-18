
namespace ControlEscolar.Models
{
    public class DetalleSolicitudViewModel
    {
        public int Id { get; set; }
        public int IdTramite { get; set; }

        public int UserId { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Grado { get; set; }
        public string Grupo { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estatus { get; set; } = string.Empty;
        public string ArchivoPath { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }
}