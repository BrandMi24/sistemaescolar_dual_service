// Services/IPdfService.cs
using ControlEscolar.Models;

namespace ControlEscolar.Services
{
    public interface IPdfService
    {
        byte[] GenerarFichaPreinscripcion(PreinscripcionEntity preinscripcion);
        byte[] GenerarFichaInscripcion(InscripcionEntity inscripcion);
    }
}