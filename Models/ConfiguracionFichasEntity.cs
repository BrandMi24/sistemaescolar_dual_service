namespace ControlEscolar.Models
{
    public class ConfiguracionFichasEntity
    {
        public int academiccontrol_inscription_ticketconfig_ID { get; set; }
        public string academiccontrol_inscription_ticketconfig_career { get; set; } = string.Empty;
        public int academiccontrol_inscription_ticketconfig_limit { get; set; }
        public DateTime academiccontrol_inscription_ticketconfig_startDate { get; set; }
        public DateTime academiccontrol_inscription_ticketconfig_endDate { get; set; }
        public bool academiccontrol_inscription_ticketconfig_status { get; set; } = true;
        public DateTime academiccontrol_inscription_ticketconfig_createdDate { get; set; }
        public DateTime academiccontrol_inscription_ticketconfig_updatedDate { get; set; }
        public int? academiccontrol_inscription_ticketconfig_inscriptionLimit { get; set; }
    }

    public class PeriodoInscripcionEntity
    {
        public int academiccontrol_inscription_period_ID { get; set; }
        public string academiccontrol_inscription_period_name { get; set; } = string.Empty;
        public DateTime academiccontrol_inscription_period_startDate { get; set; }
        public DateTime academiccontrol_inscription_period_endDate { get; set; }
        public bool academiccontrol_inscription_period_status { get; set; } = true;
        public DateTime academiccontrol_inscription_period_createdDate { get; set; }
    }
}