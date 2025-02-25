using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

            var existingCompany = await _context.Companies.Where(c => c.CompanyName == model.CompanyName).FirstOrDefaultAsync();

            if(existingCompany != null)
            {
                ViewBag.Error = "Company name already exists";
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

            await _userManager.AddToRoleAsync(user, "Owner");

            //refresh the users authentication so we can access new role
            await _signInManager.RefreshSignInAsync(user);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
            {
                throw new Exception("Invalid company ID");
            }

            var company = await _context.Companies
                .Where(c => c.Id == id)
                .Include(c => c.Sites)
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync();

            if (company == null)
            {
                throw new Exception("Invalid company ID");
            }

            // Get employee count per site
            var siteEmployeeCounts = await _context.Sites
                .Where(s => s.CompanyId == id)
                .Select(s => new
                {
                    SiteId = s.Id,
                    EmployeeCount = s.ApplicationUsers.Count()
                })
                .ToDictionaryAsync(s => s.SiteId, s => s.EmployeeCount);

            // Store employee count in ViewBag
            ViewBag.SiteEmployeeCounts = siteEmployeeCounts;

            ViewBag.SitesCount = company.Sites.Count();
            ViewBag.TotalEmployees = siteEmployeeCounts.Values.Sum(); // Total employees across all sites

            return View(company);
        }

        [HttpPost]
        public async Task<IActionResult> EditCompanyName(string companyName, int id)
        {
            if (id == 0)
            {
                throw new Exception("Cannot find site with ID");
            }

            if (companyName.IsNullOrEmpty())
            {
                throw new Exception("Error passing siteName to controller");
            }

            var company = _context.Companies.Where(c => c.Id == id).FirstOrDefault();

            if (company == null)
            {
                throw new Exception("Error: Could not find the site object");
            }

            //looking for company with that name already  
            var existingCompanyName = _context.Companies.Where(c => c.CompanyName == companyName).FirstOrDefault() ?? null;

            if (existingCompanyName != null)
            {
                ViewBag.Error = $"Error: {companyName} already exists. Choose another name";
                return RedirectToAction("Index", "Home");
            }

            //assigning new name
            company.CompanyName = companyName;

            _context.Companies.Update(company);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

    }
}
