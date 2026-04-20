namespace ControlEscolar.Models
{
    public class HistorialViewModel
    {
        public List<PreinscripcionEntity> Preinscripciones { get; set; } = new();
        public List<InscripcionEntity> Inscripciones { get; set; } = new();
        public string? FiltroCarrera { get; set; }
        public string? FiltroEstado { get; set; }
        public string? FiltroMunicipio { get; set; }
        public int? FiltroAnio { get; set; }
        public DateTime? FiltroFechaInicio { get; set; }
        public DateTime? FiltroFechaFin { get; set; }
        public List<int> AniosDisponibles { get; set; } = new();
        public string TabActiva { get; set; } = "preinscripciones";
    }
}