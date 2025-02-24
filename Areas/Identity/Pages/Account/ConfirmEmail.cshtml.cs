// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using ProRota.Hubs;
using ProRota.Models;
using ProRota.Services;
using Stripe;

namespace ProRota.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExtendedEmailSender _emailSenderService;
        private readonly IHubContext<EmailConfirmationHub> _hubContext;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, IExtendedEmailSender emailSenderService, IHubContext<EmailConfirmationHub> hubContext)
        {
            _userManager = userManager;
            _emailSenderService = emailSenderService;
            _hubContext = hubContext;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code, bool? invited)
        {
            if (userId == null || code == null)
            {
                return new JsonResult(new { success = false, message = "Invalid email confirmation request." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            if (result.Succeeded)
            {

                //Send a welcome email
                var emailBody = _emailSenderService.CreateWelcomeEmailBody(user);
                await _emailSenderService.SendEmailAsync(user.Email, "Welcome to ProRota!", emailBody);

                if (invited == true)//invite email (an employee)
                {
                    return RedirectToAction("Index", "Home", new {area = ""});
                }
                else //normal account holder 
                {
                    //delays 5 seconds so siganl R can establish a connection with the user first
                    string? connectionId = null;
                    for (int i = 0; i < 5; i++)
                    {
                        connectionId = EmailConfirmationHub.GetConnectionIdByUserId(user.Id);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            break;
                        }
                        await Task.Delay(1000); // Wait 1 second before retrying
                    }
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveConfirmation");
                        Console.WriteLine($"📌 SignalR Alert Sent to Connection ID: {connectionId}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No SignalR connection found for this user.");
                    }
                }

                return Content("Your email has been confirmed! Please close this page and head back to the application. You can now log in.");
            }

            return new JsonResult(new { success = false, message = "Error confirming email." });
        }

    }
}
