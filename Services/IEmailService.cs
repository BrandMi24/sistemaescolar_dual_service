namespace ControlEscolar.Services
{
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string asunto, string cuerpo);
        Task EnviarConAdjuntoAsync(string destinatario, string asunto, string cuerpo, byte[] adjunto, string nombreArchivo);
    }
}