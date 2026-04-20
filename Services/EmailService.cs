using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ControlEscolar.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
        {
            var remitente = _config["Email:Remitente"];
            var nombreRemitente = _config["Email:NombreRemitente"] ?? "Control Escolar";
            var host = _config["Email:Host"];
            var puertoRaw = _config["Email:Puerto"];
            var password = _config["Email:Password"];

            if (string.IsNullOrWhiteSpace(remitente) ||
                string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(puertoRaw) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Configuracion de correo incompleta en appsettings (Email:*).");
            }

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                nombreRemitente,
                remitente
            ));

            email.To.Add(MailboxAddress.Parse(destinatario));
            email.Subject = asunto;

            email.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background-color: #13322B; padding: 20px; border-radius: 8px 8px 0 0;'>
                        <h2 style='color: white; margin: 0;'>Sistema de Control Escolar</h2>
                    </div>
                    <div style='border: 1px solid #e0e0e0; padding: 20px; border-radius: 0 0 8px 8px;'>
                        <p style='color: #333;'>{cuerpo}</p>
                        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
                        <p style='color: #999; font-size: 12px;'>
                            Este es un correo automático, por favor no responda a este mensaje.
                        </p>
                    </div>
                </body>
                </html>"
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                host,
                int.Parse(puertoRaw),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                remitente,
                password
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}