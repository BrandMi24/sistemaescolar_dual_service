namespace ControlEscolar.Models
{
    public class InscripcionEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string CarreraSolicitada { get; set; } = string.Empty;
        public bool TieneMatriculaTSU { get; set; }
        public string? MatriculaTSU { get; set; }
        public string? Matricula { get; set; }
        public string? ActaNacimientoPath { get; set; }
        public string? CurpPdfPath { get; set; }
        public string? BoletaPdfPath { get; set; }
        public DateTime FechaInscripcion { get; set; }
        public string EstadoInscripcion { get; set; } = "Pendiente";

  
        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }
}