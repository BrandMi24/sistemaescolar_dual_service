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
        public int academiccontrol_preinscription_ID { get; set; }

        [Display(Name = "Folio")]
        public string? academiccontrol_preinscription_folio { get; set; }

        #region Datos Académicos y Carrera

        [Required(ErrorMessage = "La carrera solicitada es obligatoria")]
        public string academiccontrol_preinscription_careerRequested { get; set; } = string.Empty;

        public decimal? academiccontrol_preinscription_average { get; set; }

        public string? academiccontrol_preinscription_diffusionMedia { get; set; }

        // Campos auxiliares que tenías (se mantienen si los usas en lógica extra)
        [StringLength(150)]
        public string? Division { get; set; }

        [StringLength(50)]
        public string? OpcionEducativa { get; set; }

        #endregion

        #region Datos Personales (Mapeo a PreinscripcionDatosPersonalesEntity)

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string academiccontrol_preinscription_personaldata_name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        public string academiccontrol_preinscription_personaldata_paternalSurname { get; set; } = string.Empty;

        public string? academiccontrol_preinscription_personaldata_maternalSurname { get; set; }

        [Required(ErrorMessage = "El CURP es obligatorio")]
        [StringLength(18, MinimumLength = 18)]
        public string academiccontrol_preinscription_personaldata_CURP { get; set; } = string.Empty;

        public string? academiccontrol_preinscription_personaldata_maritalStatus { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime academiccontrol_preinscription_personaldata_birthDate { get; set; }

        [Required(ErrorMessage = "El sexo es obligatorio")]
        public string academiccontrol_preinscription_personaldata_gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string academiccontrol_preinscription_personaldata_email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? academiccontrol_preinscription_personaldata_phone { get; set; }

        #endregion

        #region Domicilio (Mapeo a PreinscripcionDomicilioEntity)

        [Required(ErrorMessage = "El estado es obligatorio")]
        public string academiccontrol_preinscription_address_state { get; set; } = string.Empty;

        [Required(ErrorMessage = "El municipio es obligatorio")]
        public string academiccontrol_preinscription_address_municipality { get; set; } = string.Empty;

        [Required(ErrorMessage = "La colonia es obligatoria")]
        public string academiccontrol_preinscription_address_neighborhood { get; set; } = string.Empty;

        [Required(ErrorMessage = "La calle es obligatoria")]
        public string academiccontrol_preinscription_address_street { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número es obligatorio")]
        public string academiccontrol_preinscription_address_exteriorNumber { get; set; } = string.Empty;

        [RegularExpression(@"^\d{5}$", ErrorMessage = "El CP debe tener 5 dígitos")]
        public string? academiccontrol_preinscription_address_zipCode { get; set; }

        // Campo auxiliar original
        public string? Localidad { get; set; }

        #endregion

        #region Tutor (Mapeo a PreinscripcionTutorEntity)

        [Required(ErrorMessage = "El nombre del tutor es obligatorio")]
        public string academiccontrol_preinscription_tutor_fullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El parentesco es obligatorio")]
        public string academiccontrol_preinscription_tutor_relationship { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono del tutor es obligatorio")]
        public string academiccontrol_preinscription_tutor_phone { get; set; } = string.Empty;

        #endregion

        #region Datos Escolares (Mapeo a PreinscripcionEscolarEntity)

        [Required(ErrorMessage = "La escuela de procedencia es obligatoria")]
        public string academiccontrol_preinscription_academic_originSchool { get; set; } = string.Empty;

        public string? academiccontrol_preinscription_academic_schoolState { get; set; }

        public string? academiccontrol_preinscription_academic_schoolMunicipality { get; set; }

        public string? academiccontrol_preinscription_academic_CCT { get; set; }

        public DateTime? academiccontrol_preinscription_academic_startDate { get; set; }

        public DateTime? academiccontrol_preinscription_academic_endDate { get; set; }

        #endregion

        #region Salud y Otros (Mapeo a PreinscripcionSaludEntity)

        public string? academiccontrol_preinscription_health_medicalService { get; set; }

        public bool academiccontrol_preinscription_health_hasDisability { get; set; }

        public string? academiccontrol_preinscription_health_disabilityDescription { get; set; }

        public bool academiccontrol_preinscription_health_indigenousCommunity { get; set; }

        public string? academiccontrol_preinscription_health_indigenousCommunityDescription { get; set; }

        public string? academiccontrol_preinscription_health_comments { get; set; }

        public bool academiccontrol_preinscription_health_hasChildren { get; set; }

        #endregion

        #region Campos de Control y Archivos

        public DateTime academiccontrol_preinscription_registrationDate { get; set; }

        public string academiccontrol_preinscription_state { get; set; } = "Pendiente";

        // Paths para la BD
        public string? ActaNacimientoPath { get; set; }
        public string? CurpPdfPath { get; set; }
        public string? BoletaPdfPath { get; set; }

        // Archivos físicos para el Controller
        [NotMapped] public IFormFile? ActaNacimientoFile { get; set; }
        [NotMapped] public IFormFile? CurpPdfFile { get; set; }
        [NotMapped] public IFormFile? BoletaPdfFile { get; set; }

        [NotMapped]
        public int Edad => academiccontrol_preinscription_personaldata_birthDate == default
            ? 0
            : (int)Math.Floor((DateTime.Now - academiccontrol_preinscription_personaldata_birthDate).TotalDays / 365.25);

        #endregion
    }
}
