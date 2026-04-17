using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models
{
    public class VisitaMedica
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La matrícula es obligatoria")]
        public string Matricula { get; set; } = "";

        // Estos campos se usan en la vista pero NO existen en la tabla Visitas de SQL
        [NotMapped]
        public string? NombreCompleto { get; set; }

        [NotMapped]
        public string? Carrera { get; set; }

        [NotMapped]
        public DateTime? FechaNacimiento { get; set; }

        // Datos que sí se guardan en la tabla
        public int Edad { get; set; }
        public double? Talla { get; set; }
        public double? Peso { get; set; }

        public bool TieneAlergias { get; set; }
        public string? EspecificarAlergia { get; set; }
        public string? EnfermedadesCronicas { get; set; }

        public string? FrecuenciaCardiaca { get; set; }
        public string? FrecuenciaRespiratoria { get; set; }
        public string? Saturacion { get; set; }
        public double? Temperatura { get; set; }
        public string? PresionArterial { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio")]
        public string Diagnostico { get; set; } = "";

        public DateTime FechaVisita { get; set; } = DateTime.Now;
    }
}