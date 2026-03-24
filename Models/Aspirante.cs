using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControlEscolar.Enums;

namespace ControlEscolar.Models
{
    public class Aspirante
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El folio es obligatorio")]
        [Display(Name = "Folio de aceptación")]
        [StringLength(20)]
        public string Folio { get; set; } = string.Empty;

        [NotMapped]
        [Required(ErrorMessage = "El CURP es obligatorio")]
        [Display(Name = "CURP")]
        [StringLength(18, MinimumLength = 18)]
        public string CurpValidacion { get; set; } = string.Empty;

        #region Documentos

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

        #endregion

        #region Datos del Aspirante

        // DATOS DEL ASPIRANTE
        [Required(ErrorMessage = "La carrera solicitada es obligatoria")]
        [Display(Name = "Carrera Solicitada (1ª opción)")]
        public string CarreraSolicitada { get; set; } = string.Empty;

        [Display(Name = "¿Ya cuentas con matrícula del nivel TSU en la Universidad?")]
        public bool TieneMatriculaTSU { get; set; }

        [Display(Name = "Matrícula TSU previa")]
        [StringLength(20)]
        public string? MatriculaTSU { get; set; }

        [Display(Name = "Matrícula")]
        public string? Matricula { get; set; }

        #endregion

        #region Datos Personales

        // DATOS GENERALES
        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        [Display(Name = "Apellido Paterno")]
        [StringLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Display(Name = "Apellido Materno")]
        [StringLength(100)]
        public string? ApellidoMaterno { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El sexo es obligatorio")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FechaNacimiento { get; set; }

        [Required(ErrorMessage = "La nacionalidad es obligatoria")]
        [Display(Name = "Nacionalidad")]
        public string Nacionalidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El lugar de nacimiento es obligatorio")]
        [Display(Name = "Lugar de Nacimiento")]
        public string LugarNacimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CURP es obligatorio")]
        [Display(Name = "CURP")]
        [StringLength(18, MinimumLength = 18, ErrorMessage = "El CURP debe tener 18 caracteres")]
        [RegularExpression(@"^[A-Z]{4}\d{6}[HM][A-Z]{5}[0-9A-Z]\d$", ErrorMessage = "Formato de CURP inválido")]
        public string CURP { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado civil es obligatorio")]
        [Display(Name = "Estado Civil")]
        public string EstadoCivil { get; set; } = string.Empty;

        [Display(Name = "¿Trabaja?")]
        public bool Trabaja { get; set; }

        [Display(Name = "Ocupación")]
        [StringLength(100)]
        public string? Ocupacion { get; set; }

        [Display(Name = "Lugar de trabajo")]
        [StringLength(200)]
        public string? LugarTrabajo { get; set; }

        [Display(Name = "Teléfono trabajo")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? TelefonoTrabajo { get; set; }

        #endregion

        #region Domicilio

        // DOMICILIO
        [Required(ErrorMessage = "La calle es obligatoria")]
        [Display(Name = "Calle")]
        [StringLength(200)]
        public string Calle { get; set; } = string.Empty;

        [Display(Name = "Número Ext/Int")]
        [StringLength(50)]
        public string? NumeroExtInt { get; set; }

        [Required(ErrorMessage = "La colonia es obligatoria")]
        [Display(Name = "Colonia")]
        [StringLength(100)]
        public string Colonia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El municipio es obligatorio")]
        [Display(Name = "Municipio")]
        [StringLength(100)]
        public string Municipio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [Display(Name = "Código Postal")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "El código postal debe tener 5 dígitos")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "El código postal debe contener solo números")]
        public string CodigoPostal { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [Display(Name = "e-mail")]
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Número de Seguridad Social")]
        [StringLength(20)]
        public string? NumeroSeguridadSocial { get; set; }

        #endregion

        #region Datos del Tutor

        // DATOS DEL TUTOR
        [Display(Name = "Parentesco")]
        public string? Parentesco { get; set; }

        [Display(Name = "Apellido Paterno del Tutor")]
        [StringLength(100)]
        public string? TutorApellidoPaterno { get; set; }

        [Display(Name = "Apellido Materno del Tutor")]
        [StringLength(100)]
        public string? TutorApellidoMaterno { get; set; }

        [Display(Name = "Nombre del Tutor")]
        [StringLength(100)]
        public string? TutorNombre { get; set; }

        [Display(Name = "Teléfono de casa del Tutor")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? TutorTelefonoCasa { get; set; }

        [Display(Name = "Teléfono trabajo del Tutor")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? TutorTelefonoTrabajo { get; set; }

        #endregion

        #region Datos Escolares

        // DATOS ESCOLARES
        [Required(ErrorMessage = "La escuela de procedencia es obligatoria")]
        [Display(Name = "Escuela Procedencia")]
        [StringLength(200)]
        public string EscuelaProcedencia { get; set; } = string.Empty;

        [Display(Name = "Carrera Cursada")]
        [StringLength(200)]
        public string? CarreraCursada { get; set; }

        [Display(Name = "Municipio de la Escuela")]
        [StringLength(100)]
        public string? EscuelaMunicipio { get; set; }

        [Display(Name = "Estado de la Escuela")]
        public string? EscuelaEstado { get; set; }

        [Required(ErrorMessage = "El promedio es obligatorio")]
        [Display(Name = "Promedio")]
        [Range(0, 10, ErrorMessage = "El promedio debe estar entre 0 y 10")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Formato de promedio inválido")]
        public decimal Promedio { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de terminación es obligatoria")]
        [Display(Name = "Fecha de Terminación")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FechaTerminacion { get; set; }

        [Required(ErrorMessage = "El sistema de estudio es obligatorio")]
        [Display(Name = "Sistema de estudio")]
        public string SistemaEstudio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de preparatoria es obligatorio")]
        [Display(Name = "Tipo de Preparatoria")]
        public string TipoPreparatoria { get; set; } = string.Empty;

        #endregion

        #region Otros Datos

        // OTROS DATOS
        [Display(Name = "¿Contaba con beca?")]
        public bool ContabaConBeca { get; set; }

        [Display(Name = "Tipo de beca")]
        public string? TipoBeca { get; set; }

        [Display(Name = "¿Proviene de origen indígena?")]
        public bool OrigenIndigena { get; set; }

        [Display(Name = "¿Habla lengua indígena?")]
        public bool HablaLenguaIndigena { get; set; }

        [Display(Name = "¿Cuál lengua indígena?")]
        [StringLength(100)]
        public string? LenguaIndigena { get; set; }

        [Display(Name = "¿Padece alguna discapacidad?")]
        public bool PadeceDiscapacidad { get; set; }

        [Display(Name = "Especifique discapacidad")]
        [StringLength(200)]
        public string? DiscapacidadEspecificacion { get; set; }

        [Display(Name = "¿Padece alguna enfermedad?")]
        public bool PadeceEnfermedad { get; set; }

        [Display(Name = "Especifique enfermedad")]
        [StringLength(200)]
        public string? EnfermedadEspecificacion { get; set; }

        [Display(Name = "¿Cómo se enteró de la Universidad?")]
        public string? ComoSeEntero { get; set; }

        #endregion

        #region Campos de Control

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; }

        [Display(Name = "Estado del Registro")]
        public EstadoRegistro EstadoRegistro { get; set; } = EstadoRegistro.Pendiente;

        #endregion
    }
}
