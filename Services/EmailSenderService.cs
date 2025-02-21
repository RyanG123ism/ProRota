using Microsoft.AspNetCore.Identity.UI.Services;

namespace ProRota.Services
{
    public class EmailSenderService : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // No actual email sending in development
            return Task.CompletedTask;
        }
    }
}
