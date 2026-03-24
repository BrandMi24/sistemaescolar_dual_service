using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using ControlEscolar.Enums;

namespace ControlEscolar.Models
{
    public class Preinscripcion
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Folio")]
        [StringLength(20)]
        public string? Folio { get; set; }

        #region Datos Académicos

        [Required(ErrorMessage = "La carrera solicitada es obligatoria")]
        [Display(Name = "Carrera solicitada")]
        public string CarreraSolicitada { get; set; } = string.Empty;

        [Display(Name = "División o área")]
        [StringLength(150)]
        public string? Division { get; set; }

        [Display(Name = "Opción educativa")]
        [StringLength(50)]
        public string? OpcionEducativa { get; set; }

        #endregion

        #region Datos Personales

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre(s)")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        [Display(Name = "Apellido paterno")]
        [StringLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Display(Name = "Apellido materno")]
        [StringLength(100)]
        public string? ApellidoMaterno { get; set; }

        [Required(ErrorMessage = "El CURP es obligatorio")]
        [Display(Name = "CURP")]
        [StringLength(18, MinimumLength = 18, ErrorMessage = "El CURP debe tener 18 caracteres")]
        public string CURP { get; set; } = string.Empty;

        [Display(Name = "Estado civil")]
        [StringLength(50)]
        public string? EstadoCivil { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [Display(Name = "Fecha de nacimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El sexo es obligatorio")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono de emergencia es obligatorio")]
        [Display(Name = "Teléfono de emergencia")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string TelefonoEmergencia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [Display(Name = "Correo electrónico")]
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
        public string Email { get; set; } = string.Empty;
        public string TutorNombre {  get; set; } = string.Empty;
        public string Parentesco {  get; set; }

        #endregion

        #region Domicilio

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El municipio es obligatorio")]
        [Display(Name = "Municipio")]
        [StringLength(100)]
        public string Municipio { get; set; } = string.Empty;

        [Display(Name = "Localidad")]
        [StringLength(150)]
        public string? Localidad { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria")]
        [Display(Name = "Calle")]
        [StringLength(200)]
        public string Calle { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número es obligatorio")]
        [Display(Name = "Número exterior/interior")]
        [StringLength(50)]
        public string NumeroExterior { get; set; } = string.Empty;

        [Required(ErrorMessage = "La colonia es obligatoria")]
        [Display(Name = "Colonia")]
        [StringLength(150)]
        public string Colonia { get; set; } = string.Empty;

        [Display(Name = "Código postal")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "El código postal debe tener 5 dígitos")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "El código postal debe contener solo números")]
        public string? CodigoPostal { get; set; }

        #endregion

        #region Datos Escolares

        [Required(ErrorMessage = "La escuela de procedencia es obligatoria")]
        [Display(Name = "Escuela de procedencia")]
        [StringLength(200)]
        public string? EscuelaProcedencia { get; set; }

        [Required(ErrorMessage = "El estado de la escuela es obligatorio")]
        [Display(Name = "Estado de la escuela")]
        [StringLength(100)]
        public string? EstadoEscuela { get; set; }

        [Required(ErrorMessage = "El municipio de la escuela es obligatorio")]
        [Display(Name = "Municipio de la escuela")]
        [StringLength(100)]
        public string? MunicipioEscuela { get; set; }

        [Required(ErrorMessage = "El promedio es obligatorio")]
        [Display(Name = "Promedio")]
        [Range(0, 10, ErrorMessage = "El promedio debe estar entre 0 y 10")]
        public decimal? Promedio { get; set; }

        [Display(Name = "Inicio de bachillerato")]
        [DataType(DataType.Date)]
        public DateTime? InicioBachillerato { get; set; }

        [Display(Name = "Fin de bachillerato")]
        [DataType(DataType.Date)]
        public DateTime? FinBachillerato { get; set; }

        [Display(Name = "Comentarios o información adicional")]
        [StringLength(500)]
        public string? Comentarios { get; set; }

        public string CCT {  get; set; }

        #endregion

        #region Salud y Otros

        [Display(Name = "Institución médica")]
        [StringLength(100)]
        public string? ServicioMedico { get; set; }

        [Display(Name = "Pertenece a comunidad indígena")]
        public bool ComunidadIndigena { get; set; }

        [Display(Name = "¿Cuál comunidad?")]
        [StringLength(150)]
        public string? ComunidadIndigenaDescripcion { get; set; }

        [Display(Name = "Padece alguna discapacidad")]
        public bool TieneDiscapacidad { get; set; }

        [Display(Name = "Describa la discapacidad")]
        [StringLength(250)]
        public string? DiscapacidadDescripcion { get; set; }

        [Display(Name = "¿Tiene hijos?")]
        public bool TieneHijos { get; set; }

        [Display(Name = "¿Cómo se enteró?")]
        [StringLength(150)]
        public string? MedioDifusion { get; set; }

        #endregion

        #region Campos de Control

        [Display(Name = "Fecha de preinscripción")]
        public DateTime FechaPreinscripcion { get; set; }

        [Display(Name = "Estado")]
        public EstadoPreinscripcion EstadoPreinscripcion { get; set; } = Enums.EstadoPreinscripcion.Pendiente;

        [Display(Name = "Acta de nacimiento (PDF)")]
        public string? ActaNacimientoPath { get; set; }

        [Display(Name = "CURP (PDF)")]
        public string? CurpPdfPath { get; set; }

        [Display(Name = "Boleta o certificado (PDF)")]
        public string? BoletaPdfPath { get; set; }

        [NotMapped]
        public IFormFile? ActaNacimientoFile { get; set; }

        [NotMapped]
        public IFormFile? CurpPdfFile { get; set; }

        [NotMapped]
        public IFormFile? BoletaPdfFile { get; set; }

        [NotMapped]
        [Display(Name = "Edad")]
        public int Edad => FechaNacimiento == default ? 0 : (int)Math.Floor((DateTime.Now - FechaNacimiento).TotalDays / 365.25);

        #endregion
    }
}
