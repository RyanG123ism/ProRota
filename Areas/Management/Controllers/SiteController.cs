using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef, Operations Manager")]
    [Area("Management")]
    public class SiteController : Controller
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;

        public SiteController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            //get the current site
            var siteId = GetSiteIdFromSessionOrUser();
            var site = _context.Sites.Where(s => s.Id == siteId).Include(s => s.Company).FirstOrDefault();

            if (site == null)
            {
                throw new Exception("Cannot find your site");
            }

            //send pop up message if configuration isnt complete
            if(!site.ConfigurationComplete)
            {
                ViewBag.PopUpMessage = $"It seems although {site.SiteName} still needs to be configured before ProRota can automatically create your schedules. Please use the Edit Configuration button to finish your sites configuration!";
            }

            return View(site);
        }

        [HttpPost]
        public async Task<IActionResult> EditConfiguration(int coversCapacity, int numberOfSections, int maxFrontOfHouse, int maxBartenders, int maxManagement, int minManagement)
        {
            var siteId = GetSiteIdFromSessionOrUser();
            var site = _context.Sites.Find(siteId);

            if (site == null) 
            {
                throw new Exception("Site could not be found");
            }

            //updating attributes
            site.CoversCapacity = coversCapacity;
            site.NumberOfSections = numberOfSections;
            site.MaxFrontOfHouse = maxFrontOfHouse;
            site.MaxBarTenders = maxBartenders;
            site.MaxManagement = maxManagement;
            site.MinManagement = minManagement;

            //setting the config status to true
            site.ConfigurationComplete = true;

            //update entity and save to db 
            _context.Sites.Update(site);
            var result = await _context.SaveChangesAsync();
            if (result <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            };

            ViewBag.PopUpMessage = "Site Configuration Updated!";
            return RedirectToAction("Index");
        }

        public int GetSiteIdFromSessionOrUser()
        {
            var siteId = HttpContext.Session.GetInt32("AdminsCurrentSiteId");

            if (siteId == null)
            {
                //gets current users ID and then gets the user object
                var userId = _userManager.GetUserId(User);
                var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == userId);

                //if not found
                if (user == null)
                {
                    throw new Exception("Current user not found.");
                }

                if (user.SiteId == null)
                {
                    throw new Exception("Current site not found.");
                }

                siteId = user.SiteId;
            }

            return (int)siteId;
        }
    }
}
