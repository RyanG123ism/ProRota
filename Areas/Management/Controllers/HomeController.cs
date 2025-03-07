using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;

namespace ProRota.Areas.Management.Controllers
{
    [Area("Management")]
    [Authorize(Roles = "Owner, Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    public class HomeController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private IClaimsService _claimsService;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IClaimsService claimsService)
        {
            _context = context;
            _userManager = userManager;
            _claimsService = claimsService;

        }

        public async Task<IActionResult> Index(int siteId = 0)//siteId is only passed in if an Admin/Owner is logged in
        {
            //if its a non admin/owner user 
            if(siteId == 0)
            {
                //get the current user's ID
                var userId = _userManager.GetUserId(User);

                //retrieve the user object with site
                var currentUser = await _context.ApplicationUsers
                    .Include(u => u.Site)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                // Pass the site name to the view
                ViewBag.SiteName = currentUser?.Site?.SiteName ?? "Unknown Site";
            }
            else
            {
                if(User.IsInRole("Owner") || User.IsInRole("Admin"))
                {
                    //teminates any current sessions running for admin
                    HttpContext.Session.Remove("UsersCurrentSite");

                    var site = _context.Sites.Find(siteId);
                    ViewBag.SiteName = site.SiteName;

                    //creates a session for the admin to store the current site that they are viewing
                    HttpContext.Session.SetInt32("UsersCurrentSite", siteId);
                }            
            }

            //get the company info and pass to view
            var companyId = _claimsService.GetCompanyId();

            if (companyId == null)
            {
                throw new Exception("Cannot find company Id");
            }

            var company = await _context.Companies.Where(c => c.Id == companyId).FirstOrDefaultAsync();
            ViewBag.CompanyName = company.CompanyName;

            return View();
        }
    }
}
