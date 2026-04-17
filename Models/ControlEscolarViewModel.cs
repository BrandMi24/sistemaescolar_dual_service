//namespace ControlEscolar.Models
//{
//    public class ControlEscolarViewModel
//    {
//        public string Tab { get; set; } = "preinscripciones";
//        public List<PreinscripcionEntity> Preinscripciones { get; set; } = new();
//        public List<InscripcionEntity> Inscripciones { get; set; } = new();
//        public string? FiltroCarrera { get; set; }
//        public string? FiltroEstado { get; set; }
//    }
//}

namespace ControlEscolar.Models
{
    public class ControlEscolarViewModel
    {
        // Control de navegación en la vista
        public string Tab { get; set; } = "preinscripciones";

        // Listas de las entidades con los nombres de clase corregidos
        public List<PreinscripcionEntity> Preinscripciones { get; set; } = new();
        public List<InscripcionEntity> Inscripciones { get; set; } = new();

        // Propiedades para los filtros de búsqueda
        public string? FiltroCarrera { get; set; }
        public string? FiltroEstado { get; set; }
    }
}