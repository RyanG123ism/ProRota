using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Areas.Management.Controllers
{
    public class HomeController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager /*RoleManager<IdentityRole> roleManager*/)
        {
            _context = context;
            _userManager = userManager;

        }

        [Area("Management")]
        public async Task<IActionResult> Index(int siteId = 0)
        {

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
                //teminates any current sessions running for admin
                HttpContext.Session.Remove("AdminsCurrentSiteId");

                var site = _context.Sites.Find(siteId);
                ViewBag.SiteName = site.SiteName;

                //creates a session for the admin to store the current site that they are viewing
                HttpContext.Session.SetInt32("AdminsCurrentSiteId", siteId);
            }      

            return View();
        }
    }
}
