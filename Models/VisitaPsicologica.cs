using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlEscolar.Models
{
    public class VisitaPsicologica
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La matrícula es obligatoria")]
        public string Matricula { get; set; } = "";

        [NotMapped]
        public string? NombreCompleto { get; set; }

        [NotMapped]
        public string? Carrera { get; set; }

        [NotMapped]
        public DateTime? FechaNacimiento { get; set; }

        public int Edad { get; set; }
        public DateTime FechaVisita { get; set; } = DateTime.Now;

        public bool TerapiaPrevia { get; set; }
        public string? MotivoConsultaPrevia { get; set; }
        public string? MedicacionPsiquiatrica { get; set; }

        [Required(ErrorMessage = "El motivo de consulta es obligatorio")]
        public string MotivoConsulta { get; set; } = "";
    }
}