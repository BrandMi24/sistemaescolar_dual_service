namespace ControlEscolar.Models
{
    public class ControlEscolarViewModel
    {
        public string Tab { get; set; } = "preinscripciones";
        public List<PreinscripcionEntity> Preinscripciones { get; set; } = new();
        public List<InscripcionEntity> Inscripciones { get; set; } = new();
        public string? FiltroCarrera { get; set; }
        public string? FiltroEstado { get; set; }
    }
}