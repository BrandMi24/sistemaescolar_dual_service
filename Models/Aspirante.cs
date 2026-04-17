using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ControlEscolar.Enums;
using Microsoft.AspNetCore.Http;

namespace ControlEscolar.Models
{
    public class Aspirante
    {
        [Key]
        public int academiccontrol_inscription_ID { get; set; }

        // Campos de Validación de entrada
        public string FolioValidacion { get; set; } = string.Empty;
        public string CurpValidacion { get; set; } = string.Empty;

        #region Datos de Inscripción
        public string academiccontrol_inscription_careerRequested { get; set; } = string.Empty;
        public bool academiccontrol_inscription_hasTSUEnrollment { get; set; }
        public string? academiccontrol_inscription_TSUEnrollment { get; set; }
        public string? academiccontrol_inscription_enrollment { get; set; }
        public DateTime academiccontrol_inscription_registrationDate { get; set; }
        public string academiccontrol_inscription_state { get; set; } = "Pendiente";
        public int academiccontrol_inscription_preinscriptionID { get; set; }
        #endregion

        #region Datos Personales (Mapeo a PreinscripcionDatosPersonalesEntity)
        public string academiccontrol_preinscription_personaldata_name { get; set; } = string.Empty;
        public string academiccontrol_preinscription_personaldata_paternalSurname { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_maternalSurname { get; set; }
        public string academiccontrol_preinscription_personaldata_CURP { get; set; } = string.Empty;
        public DateTime academiccontrol_preinscription_personaldata_birthDate { get; set; }
        public string academiccontrol_preinscription_personaldata_gender { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_maritalStatus { get; set; }
        public string academiccontrol_preinscription_personaldata_email { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_phone { get; set; }

        // Campos adicionales que tenías en tu original
        public string? Nacionalidad { get; set; }
        public string? LugarNacimiento { get; set; }
        public bool Trabaja { get; set; }
        public string? Ocupacion { get; set; }
        public string? LugarTrabajo { get; set; }
        public string? TelefonoTrabajo { get; set; }
        #endregion

        #region Domicilio (Mapeo a PreinscripcionDomicilioEntity)
        public string academiccontrol_preinscription_address_state { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_municipality { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_address_zipCode { get; set; }
        public string academiccontrol_preinscription_address_neighborhood { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_street { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_exteriorNumber { get; set; } = string.Empty;
        public string? NumeroSeguridadSocial { get; set; }
        #endregion

        #region Datos del Tutor (Mapeo a PreinscripcionTutorEntity)
        public string academiccontrol_preinscription_tutor_fullName { get; set; } = string.Empty;
        public string academiccontrol_preinscription_tutor_relationship { get; set; } = string.Empty;
        public string academiccontrol_preinscription_tutor_phone { get; set; } = string.Empty;
        #endregion

        #region Datos Escolares (Mapeo a PreinscripcionEscolarEntity)
        public string academiccontrol_preinscription_academic_originSchool { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_academic_schoolState { get; set; }
        public string? academiccontrol_preinscription_academic_schoolMunicipality { get; set; }
        public string? academiccontrol_preinscription_academic_CCT { get; set; }
        public DateTime? academiccontrol_preinscription_academic_startDate { get; set; }
        public DateTime? academiccontrol_preinscription_academic_endDate { get; set; }
        public decimal academiccontrol_preinscription_average { get; set; }
        public string? SistemaEstudio { get; set; }
        public string? TipoPreparatoria { get; set; }
        #endregion

        #region Salud y Otros (Mapeo a PreinscripcionSaludEntity)
        public string? academiccontrol_preinscription_health_medicalService { get; set; }
        public bool academiccontrol_preinscription_health_hasDisability { get; set; }
        public string? academiccontrol_preinscription_health_disabilityDescription { get; set; }
        public bool academiccontrol_preinscription_health_indigenousCommunity { get; set; }
        public string? academiccontrol_preinscription_health_indigenousCommunityDescription { get; set; }
        public string? academiccontrol_preinscription_health_comments { get; set; }
        public bool academiccontrol_preinscription_health_hasChildren { get; set; }

        // Otros datos adicionales del original
        public bool ContabaConBeca { get; set; }
        public string? TipoBeca { get; set; }
        public string? ComoSeEntero { get; set; }
        #endregion

        #region Documentos (Files)
        [NotMapped] public IFormFile? ActaNacimientoFile { get; set; }
        [NotMapped] public IFormFile? CurpPdfFile { get; set; }
        [NotMapped] public IFormFile? BoletaPdfFile { get; set; }

        public string? academiccontrol_inscription_birthCertificatePath { get; set; }
        public string? academiccontrol_inscription_curpPdfPath { get; set; }
        public string? academiccontrol_inscription_transcriptPath { get; set; }
        #endregion

        [NotMapped]
        public PreinscripcionEntity? Preinscripcion { get; set; }
    }
}
