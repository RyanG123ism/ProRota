using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProRota.Areas.Admin.Models.ViewModels;
using ProRota.Data;
using ProRota.Models;
using System.Security.Claims;

namespace ProRota.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Owner, Partial_User_Paid")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateCompany()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.ApplicationUsers.FindAsync(userId);

            if (user == null)
            {
                throw new Exception("User object could not be found");
            }

            var model = new CreateCompanyViewModel
            {
                ApplicationUserId = user.Id,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany(CreateCompanyViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model); 
            }

            var user = await _context.ApplicationUsers.FindAsync(model.ApplicationUserId);

            var company = new Company
            {
                CompanyName = model.CompanyName,
                ApplicationUserId = model.ApplicationUserId,
                Sites = new List<Site>()
            };

            await _context.Companies.AddAsync(company);
            await _context.SaveChangesAsync();

            if (User.IsInRole("Partial_User_Paid"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Partial_User_Paid");
            }

            await _userManager.AddToRoleAsync(user, "owner");

            //refresh the users authentication so we can access new role
            await _signInManager.RefreshSignInAsync(user);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
