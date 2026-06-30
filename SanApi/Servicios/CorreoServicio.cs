using System.Net;
using System.Net.Mail;

namespace SanApi.Servicios
{
    public interface ICorreoServicio
    {
        Task EnviarCodigoVerificacionAsync(string correoDestino, string nombreUsuario, string codigo);
    }
    public class CorreoServicio: ICorreoServicio
    {
        private readonly IConfiguration _configuration;

        public CorreoServicio(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCodigoVerificacionAsync(string correoDestino, string nombreUsuario, string codigo)
        {
            var smtpServer = _configuration["EmailSettings:Server"];
            var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var password = _configuration["EmailSettings:Password"];

            using (var message = new MailMessage())
            {
                // fromEmail es lo que ve el usuario ("Gestor de San <tu-correo@gmail.com>")
                message.From = new MailAddress(fromEmail!, "Gestor de San");
                message.To.Add(new MailAddress(correoDestino));
                message.Subject = "Código de Verificación - Gestor de San";

                message.Body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                    <h2 style='color: #0f172a; text-align: center; margin-bottom: 20px;'>¡Hola, {nombreUsuario}!</h2>
                    <p style='font-size: 16px; color: #334155; line-height: 1.5; text-align: center;'>
                        Gracias por unirte al Gestor de San. Para completar tu registro y activar tu cuenta, introduce el siguiente código de verificación en la aplicación:
                    </p>
                    <div style='background-color: #f1f5f9; padding: 20px; text-align: center; font-size: 36px; font-weight: bold; letter-spacing: 6px; color: #16a34a; margin: 30px 0; border-radius: 8px; border: 1px dashed #cbd5e1;'>
                        {codigo}
                    </div>
                    <p style='font-size: 13px; color: #64748b; text-align: center;'>
                        Este código es de un solo uso. Si no solicitaste este registro, ignora este mensaje.
                    </p>
                </div>";
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(smtpServer, port))
                {
                    // smtpUser es la credencial estricta de la red de Brevo
                    client.Credentials = new NetworkCredential(smtpUser, password);
                    client.EnableSsl = true;

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}
