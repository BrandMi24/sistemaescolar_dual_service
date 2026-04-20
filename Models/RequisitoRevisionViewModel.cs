//TRAMITES
namespace ControlEscolar.Models
{
    public class RequisitoRevisionViewModel
    {
        public int IdRequisito { get; set; }
        public string? NombreRequisito { get; set; }
        public bool? EsFotografia { get; set; }

        public string? NombreArchivoFisico { get; set; }
        public bool? ArchivoExiste { get; set; }

        public string? Estatus { get; set; }
    }
}