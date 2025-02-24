using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using ProRota.Models;

namespace ProRota.Services
{
    public interface IExtendedEmailSender : IEmailSender
    {
        public string CreateInviteEmailBody(ApplicationUser user, string role, string siteName, string callbackUrl);
        public string CreateEmailConfirmationBody(ApplicationUser user, string callbackUrl);
        public string CreateWelcomeEmailBody(ApplicationUser user);
    }
}
