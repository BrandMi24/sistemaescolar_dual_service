using Microsoft.AspNetCore.Http;

namespace ControlEscolar.Models
{
    public class CorreccionDocumentosViewModel
    {
        public int InscripcionId { get; set; }
        public string Folio { get; set; } = string.Empty;
        public string Curp { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;

        // Qué documentos tienen error activo
        public bool ActaConError { get; set; }
        public bool CurpConError { get; set; }
        public bool BoletaConError { get; set; }
        public string? MotivoError { get; set; }

        // Archivos que el alumno sube (solo los con error)
        public IFormFile? ActaNacimientoFile { get; set; }
        public IFormFile? CurpPdfFile { get; set; }
        public IFormFile? BoletaPdfFile { get; set; }
    }
}
