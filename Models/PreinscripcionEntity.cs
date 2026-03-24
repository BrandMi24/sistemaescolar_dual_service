namespace ControlEscolar.Models
{
    public class PreinscripcionEntity
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
        public string CarreraSolicitada { get; set; } = string.Empty;
        public decimal Promedio { get; set; }
        public string? MedioDifusion { get; set; }
        public DateTime FechaPreinscripcion { get; set; }
        public string EstadoPreinscripcion { get; set; } = "Pendiente";

   
        public PreinscripcionDatosPersonalesEntity? DatosPersonales { get; set; }
        public PreinscripcionDomicilioEntity? Domicilio { get; set; }
        public PreinscripcionTutorEntity? Tutor { get; set; }
        public PreinscripcionEscolarEntity? DatosEscolares { get; set; }
        public PreinscripcionSaludEntity? Salud { get; set; }
    }

    public class PreinscripcionDatosPersonalesEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string CURP { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string? EstadoCivil { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionDomicilioEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string? CodigoPostal { get; set; }
        public string Colonia { get; set; } = string.Empty;
        public string Calle { get; set; } = string.Empty;
        public string NumeroExterior { get; set; } = string.Empty;

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionTutorEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string TutorNombre { get; set; } = string.Empty;
        public string Parentesco { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionEscolarEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string EscuelaProcedencia { get; set; } = string.Empty;
        public string? EstadoEscuela { get; set; }
        public string? MunicipioEscuela { get; set; }
        public string? CCT { get; set; }
        public DateTime? InicioBachillerato { get; set; }
        public DateTime? FinBachillerato { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }

    public class PreinscripcionSaludEntity
    {
        public int Id { get; set; }
        public int PreinscripcionId { get; set; }
        public string? ServicioMedico { get; set; }
        public bool TieneDiscapacidad { get; set; }
        public string? DiscapacidadDescripcion { get; set; }
        public bool ComunidadIndigena { get; set; }
        public string? ComunidadIndigenaDescripcion { get; set; }
        public string? Comentarios { get; set; }
        //public bool TieneHijos { get; set; }

        public PreinscripcionEntity Preinscripcion { get; set; } = null!;
    }
}