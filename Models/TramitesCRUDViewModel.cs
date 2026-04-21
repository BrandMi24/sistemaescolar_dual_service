namespace ControlEscolar.Models
{
    public class TramitesCRUDViewModel
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public List<RequisitoDTO> listaRequisitos { get; set; }
    }

    public class RequisitoDTO
    {
        public string nombre { get; set; }
    }
}