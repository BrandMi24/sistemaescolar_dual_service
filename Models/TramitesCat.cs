namespace ControlEscolar.Models
{
    public class TramitesCat
    {
        public IEnumerable<DetalleSolicitudViewModel> Historial { get; set; }
        public IEnumerable<Cat_Tramites> Categorias { get; set; }
    }
}
