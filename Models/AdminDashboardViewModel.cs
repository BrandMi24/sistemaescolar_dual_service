
namespace ControlEscolar.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalPreinscripciones { get; set; }
        public int TotalAspirantes { get; set; }
        public int PreinscripcionesPendientes { get; set; }
        public int PreinscripcionesConvertidas { get; set; }
        public int PreinscripcionesCanceladas { get; set; }
        public int AspirantesPendientes { get; set; }
        public int AspirantesAprobados { get; set; }
        public int AspirantesRechazados { get; set; }
        public Dictionary<string, int> PreinscripcionesPorCarrera { get; set; } = new();
        public Dictionary<string, int> AspirantesPorCarrera { get; set; } = new();
        public Dictionary<string, int> PreinscripcionesPorEstado { get; set; } = new();
        public Dictionary<string, int> AspirantesPorEstado { get; set; } = new();
        public List<PreinscripcionEntity> UltimasPreinscripciones { get; set; } = new();
        public List<InscripcionEntity> UltimosAspirantes { get; set; } = new();
    }
}
