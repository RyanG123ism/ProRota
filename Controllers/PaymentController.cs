using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using SQLitePCL;
using System.Security.Claims;

namespace ProRota.Controllers
{
    public class PaymentController : Controller
    {
        private readonly StripePaymentService _stripeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public PaymentController(StripePaymentService stripeService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, SignInManager<ApplicationUser> signInManager)
        {
            _stripeService = stripeService;
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string plan, decimal price)
        {
            if(price <= 0 )
            {
                return BadRequest("Invalid price.");
            }

            string successUrl = Url.Action("Success", "Payment", null, Request.Scheme);
            string cancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme);

            //returns the url based on the success of the payment
            string sessionUrl = await _stripeService.CreateCheckoutSession(price, "gbp", successUrl, cancelUrl, plan);

            return Redirect(sessionUrl);
        }

        public async Task<IActionResult> Success()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.ApplicationUsers.FindAsync(userId);

            if(User.IsInRole("Partial_User_Unpaid"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Partial_User_Unpaid");
            }

            //update role
            await _userManager.AddToRoleAsync(user, "Partial_User_Paid");

            //refresh the users authentication so we can access new role
            await _signInManager.RefreshSignInAsync(user);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
