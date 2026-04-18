namespace ControlEscolar.Models
{
    public class ConfiguracionViewModel
    {
        public List<ConfiguracionFichasEntity> Configuraciones { get; set; } = new();
        public List<PeriodoInscripcionEntity> Periodos { get; set; } = new();
    }
}