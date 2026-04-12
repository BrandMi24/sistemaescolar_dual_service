using ControlEscolar.Models;

namespace ControlEscolar.Models;

public class InscripcionEntity
{
    public int academiccontrol_inscription_ID { get; set; }
    public int academiccontrol_inscription_preinscriptionID { get; set; }
    public string academiccontrol_inscription_careerRequested { get; set; } = string.Empty;
    public bool academiccontrol_inscription_hasTSUEnrollment { get; set; }
    public string? academiccontrol_inscription_TSUEnrollment { get; set; }
    public string? academiccontrol_inscription_enrollment { get; set; }
    public string? academiccontrol_inscription_birthCertificatePath { get; set; }
    public string? academiccontrol_inscription_curpPdfPath { get; set; }
    public string? academiccontrol_inscription_transcriptPath { get; set; }
    public DateTime academiccontrol_inscription_registrationDate { get; set; }
    public string academiccontrol_inscription_state { get; set; } = "Pendiente";
    public bool academiccontrol_inscription_status { get; set; } = true;
    public DateTime academiccontrol_inscription_createdDate { get; set; }

    public PreinscripcionEntity Preinscripcion { get; set; } = null!;
}