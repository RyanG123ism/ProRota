using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;

namespace ProRota.Services
{
    public class EmailSenderService : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSenderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUser = _configuration["EmailSettings:SmtpUser"];
                var smtpPass = _configuration["EmailSettings:SmtpPass"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;  // ✅ Ensure Gmail credentials are used

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine("✅ Email sent successfully!");  // Debugging
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email send failed: {ex.Message}");
            }
        }

    }
}
