using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;
using ProRota.Models;
using System.Text.Encodings.Web;

namespace ProRota.Services
{
    public class EmailSenderService : IExtendedEmailSender
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

        public string CreateInviteEmailBody(ApplicationUser user, string role, string siteName, string callbackUrl)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            padding: 20px;
                        }}
                        .container {{
                            background-color: #ffffff;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0px 0px 10px 2px #ddd;
                            max-width: 600px;
                            margin: auto;
                        }}
                        h2 {{
                            color: #2c3e50;
                        }}
                        p {{
                            color: #34495e;
                        }}
                        .details {{
                            margin-top: 20px;
                            padding: 15px;
                            background-color: #ecf0f1;
                            border-left: 5px solid #3498db;
                            font-size: 16px;
                        }}
                        .button {{
                            display: block;
                            width: 100%;
                            max-width: 200px;
                            margin: 20px auto;
                            padding: 10px;
                            text-align: center;
                            background-color: #3498db;
                            color: white;
                            text-decoration: none;
                            font-size: 18px;
                            border-radius: 5px;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 12px;
                            color: #7f8c8d;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Employment Invitation</h2>
                        <p>Dear {user.FirstName} {user.LastName},</p>
                        <p>
                            We are pleased to invite you to join <strong>ProRota</strong>. Below are the details of your proposed position.
                            Please review the information and confirm your account by clicking the button below.
                        </p>

                        <div class='details'>
                            <strong>Employee Name:</strong> {user.FirstName} {user.LastName} <br>
                            <strong>Email:</strong> {user.Email} <br>
                            <strong>Salary:</strong> £{user.Salary:N2} per hour <br>
                            <strong>Contractual Hours:</strong> {user.ContractualHours} hours per week <br>
                            <strong>Site:</strong> {siteName} <br>
                            <strong>Position:</strong> {role} <br>
                        </div>

                        <p style='text-align:center;'>
                            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='button'>Confirm & Accept</a>
                        </p>

                        <p>
                            If you did not expect this invitation, please ignore this email or contact your administrator.
                        </p>

                        <div class='footer'>
                            <p>ProRota | Workforce Management System</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public string CreateEmailConfirmationBody(ApplicationUser user, string callbackUrl)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            padding: 20px;
                        }}
                        .container {{
                            background-color: #ffffff;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0px 0px 10px 2px #ddd;
                            max-width: 600px;
                            margin: auto;
                        }}
                        h2 {{
                            color: #2c3e50;
                        }}
                        p {{
                            color: #34495e;
                        }}
                        .button {{
                            display: block;
                            width: 100%;
                            max-width: 200px;
                            margin: 20px auto;
                            padding: 10px;
                            text-align: center;
                            background-color: #3498db;
                            color: white;
                            text-decoration: none;
                            font-size: 18px;
                            border-radius: 5px;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 12px;
                            color: #7f8c8d;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Email Confirmation</h2>
                        <p>Dear {user.FirstName} {user.LastName},</p>
                        <p>
                            Thank you for registering with <strong>ProRota</strong>. Before you can access your account,
                            you need to verify your email address.
                        </p>

                        <p>Please confirm your email by clicking the button below:</p>

                        <p style='text-align:center;'>
                            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='button'>Confirm Email</a>
                        </p>

                        <p>
                            If you did not register for an account, please ignore this email.
                        </p>

                        <div class='footer'>
                            <p>ProRota | Workforce Management System</p>
                        </div>
                    </div>
                </body>
                </html>";
        }


        public string CreateWelcomeEmailBody(ApplicationUser user)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            padding: 20px;
                        }}
                        .container {{
                            background-color: #ffffff;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0px 0px 10px 2px #ddd;
                            max-width: 600px;
                            margin: auto;
                        }}
                        h2 {{
                            color: #2c3e50;
                        }}
                        p {{
                            color: #34495e;
                        }}
                        .button {{
                            display: block;
                            width: 100%;
                            max-width: 200px;
                            margin: 20px auto;
                            padding: 10px;
                            text-align: center;
                            background-color: #28a745;
                            color: white;
                            text-decoration: none;
                            font-size: 18px;
                            border-radius: 5px;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 12px;
                            color: #7f8c8d;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Welcome to ProRota, {user.FirstName}!</h2>
                        <p>We are excited to have you on board.</p>
                        <p>ProRota is here to help you manage your work schedules efficiently and keep track of your shifts with ease.</p>

                        <p><strong>Here’s how you can get started:</strong></p>
                        <ul>
                            <li>✔ Log in to your dashboard.</li>
                            <li>✔ Set up your profile and preferences.</li>
                            <li>✔ View your assigned shifts.</li>
                            <li>✔ Request time off and swap shifts easily.</li>
                        </ul>

                        <p style='text-align:center;'>
                            <a href='https://yourappurl.com/Identity/Account/Login' class='button'>Log in to Your Account</a>
                        </p>

                        <p>If you have any questions, feel free to reach out to our support team.</p>

                        <div class='footer'>
                            <p>ProRota | Workforce Management System</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

    }
}
