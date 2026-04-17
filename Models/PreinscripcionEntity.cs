namespace ControlEscolar.Models
{
    public class PreinscripcionEntity
    {
        public int academiccontrol_preinscription_ID { get; set; }
        public string? academiccontrol_preinscription_folio { get; set; }
        public string academiccontrol_preinscription_careerRequested { get; set; } = string.Empty;
        public decimal academiccontrol_preinscription_average { get; set; }
        public string? academiccontrol_preinscription_diffusionMedia { get; set; }
        public DateTime academiccontrol_preinscription_registrationDate { get; set; }
        public string academiccontrol_preinscription_state { get; set; } = "Pendiente";
        public bool academiccontrol_preinscription_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_createdDate { get; set; }

        // Navigation properties
        public PreinscripcionDatosPersonalesEntity? DatosPersonales { get; set; }
        public PreinscripcionDomicilioEntity? Domicilio { get; set; }
        public PreinscripcionTutorEntity? Tutor { get; set; }
        public PreinscripcionEscolarEntity? DatosEscolares { get; set; }
        public PreinscripcionSaludEntity? Salud { get; set; }
    }

    public class PreinscripcionDatosPersonalesEntity
    {
        public int academiccontrol_preinscription_personaldata_ID { get; set; }
        public int academiccontrol_preinscription_personaldata_preinscriptionID { get; set; }
        public string academiccontrol_preinscription_personaldata_name { get; set; } = string.Empty;
        public string academiccontrol_preinscription_personaldata_paternalSurname { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_maternalSurname { get; set; }
        public string academiccontrol_preinscription_personaldata_CURP { get; set; } = string.Empty;
        public DateTime academiccontrol_preinscription_personaldata_birthDate { get; set; }
        public string academiccontrol_preinscription_personaldata_gender { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_maritalStatus { get; set; }
        public string academiccontrol_preinscription_personaldata_email { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_personaldata_phone { get; set; }
        public bool academiccontrol_preinscription_personaldata_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_personaldata_createdDate { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionDomicilioEntity
    {
        public int academiccontrol_preinscription_address_ID { get; set; }
        public int academiccontrol_preinscription_address_preinscriptionID { get; set; }
        public string academiccontrol_preinscription_address_state { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_municipality { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_address_zipCode { get; set; }
        public string academiccontrol_preinscription_address_neighborhood { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_street { get; set; } = string.Empty;
        public string academiccontrol_preinscription_address_exteriorNumber { get; set; } = string.Empty;
        public bool academiccontrol_preinscription_address_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_address_createdDate { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionTutorEntity
    {
        public int academiccontrol_preinscription_tutor_ID { get; set; }
        public int academiccontrol_preinscription_tutor_preinscriptionID { get; set; }
        public string academiccontrol_preinscription_tutor_fullName { get; set; } = string.Empty;
        public string academiccontrol_preinscription_tutor_relationship { get; set; } = string.Empty;
        public string academiccontrol_preinscription_tutor_phone { get; set; } = string.Empty;
        public bool academiccontrol_preinscription_tutor_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_tutor_createdDate { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionEscolarEntity
    {
        public int academiccontrol_preinscription_academic_ID { get; set; }
        public int academiccontrol_preinscription_academic_preinscriptionID { get; set; }
        public string academiccontrol_preinscription_academic_originSchool { get; set; } = string.Empty;
        public string? academiccontrol_preinscription_academic_schoolState { get; set; }
        public string? academiccontrol_preinscription_academic_schoolMunicipality { get; set; }
        public string? academiccontrol_preinscription_academic_CCT { get; set; }
        public DateTime? academiccontrol_preinscription_academic_startDate { get; set; }
        public DateTime? academiccontrol_preinscription_academic_endDate { get; set; }
        public bool academiccontrol_preinscription_academic_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_academic_createdDate { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionSaludEntity
    {
        public int academiccontrol_preinscription_health_ID { get; set; }
        public int academiccontrol_preinscription_health_preinscriptionID { get; set; }
        public string? academiccontrol_preinscription_health_medicalService { get; set; }
        public bool academiccontrol_preinscription_health_hasDisability { get; set; }
        public string? academiccontrol_preinscription_health_disabilityDescription { get; set; }
        public bool academiccontrol_preinscription_health_indigenousCommunity { get; set; }
        public string? academiccontrol_preinscription_health_indigenousCommunityDescription { get; set; }
        public string? academiccontrol_preinscription_health_comments { get; set; }
        public bool academiccontrol_preinscription_health_hasChildren { get; set; }
        public bool academiccontrol_preinscription_health_status { get; set; } = true;
        public DateTime academiccontrol_preinscription_health_createdDate { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }
}