using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Owner, Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class SiteController : Controller
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly INewsFeedService _newsFeedService;

        public SiteController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, INewsFeedService newsFeedService)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _newsFeedService = newsFeedService;
        }

        public IActionResult Index()
        {
            //get the current site
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = _context.Sites.Where(s => s.Id == siteId)
                .Include(s => s.Company)
                .Include(s => s.SiteConfiguration)
                .Include(s => s.SiteConfiguration.RoleConfigurations).SingleOrDefault();

            if (site == null)
            {
                throw new Exception("Cannot find your site");
            }

            if (TempData["popUpMessage"] == null)
            {
                //send pop up message if configuration hasnt been done
                if (site.SiteConfiguration == null)
                {
                    ViewBag.Alert = $"It seems although {site.SiteName} still needs to be configured before ProRota can automatically create your schedules. Please use the Edit Configuration button to finish your sites configuration!";
                }
            }
            else
            {
                ViewBag.Success = TempData["popUpMessage"];
            }
            

            return View(site);
        }

        [HttpPost]
        public async Task<IActionResult> EditConfiguration(TimeSpan bookingDuration, int coversCapacity, int numberOfSections)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = _context.Sites.Find(siteId);

            if (site == null) 
            {
                throw new Exception("Site could not be found");
            }

            var siteConfig = _context.SiteConfigurations.Where(s => s.SiteId == site.Id).FirstOrDefault();

            //if a confiuration already exists
            if(site.SiteConfiguration != null)
            {
                siteConfig.BookingDuration = bookingDuration;
                siteConfig.CoversCapacity = coversCapacity;
                siteConfig.NumberOfSections = numberOfSections;

                _context.SiteConfigurations.Update(siteConfig);
            }
            else
            {
                //create new configuration
                var config = new SiteConfiguration
                {
                    BookingDuration = bookingDuration,
                    CoversCapacity = coversCapacity,
                    NumberOfSections = numberOfSections,
                    SiteId = site.Id //attaching to current site
                };

                //add new entity
                _context.SiteConfigurations.Add(config);
            }
            
            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            };

            //sending message back to the index action
            TempData["popUpMessage"] = "Site Configuration Updated!";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> EditTradingTimes(DateTime openingTime, DateTime closingTime, int dayOfWeekEnum)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);

            if (site == null)
            {
                throw new Exception("Site could not be found");
            }

            if(openingTime == null || closingTime == null) 
            {
                throw new Exception("Can't find opening or closing times");
            }

            if(openingTime > closingTime)
            {
                throw new Exception("Opening time is greater than closing time");
            }

            TimeSpan oTime = openingTime.TimeOfDay;
            TimeSpan cTime = closingTime.TimeOfDay;

            //assigns the opening and closing times to the site based on the dayofweek enum value
            switch(dayOfWeekEnum)
            {
                case 0:
                    site.SundayOpenTime = openingTime;
                    site.SundayCloseTime = closingTime;
                    break;
                case 1:
                    site.MondayOpenTime = openingTime;
                    site.MondayCloseTime = closingTime;
                    break;
                case 2:
                    site.TuesdayOpenTime = openingTime;
                    site.TuesdayCloseTime = closingTime;
                    break;
                case 3:
                    site.WednesdayOpenTime = openingTime;
                    site.WednesdayCloseTime = closingTime;
                    break;
                case 4:
                    site.ThursdayOpenTime = openingTime;
                    site.ThursdayCloseTime = closingTime;
                    break;
                case 5:
                    site.FridayOpenTime = openingTime;
                    site.FridayCloseTime = closingTime;
                    break;
                case 6:
                    site.SaturdayOpenTime = openingTime;
                    site.SaturdayCloseTime = closingTime;
                    break;
            }

            //update entity and save to db 
            _context.Sites.Update(site);
            var result = await _context.SaveChangesAsync();
            if (result <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            };

            //create post and notify user
            await _newsFeedService.createAndPostNewsFeedItem(
                $"Trading days/hours have been changed. Make sure to double check your shifts!", site.Id);

            TempData["popUpMessage"] = "Trading Hours Updated!";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> RemoveTradingTimes(int dayOfWeekEnum2)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);

            if (site == null)
            {
                throw new Exception("Site could not be found");
            }

            //removes the opening and closing times of a day depending on the enum value
            switch (dayOfWeekEnum2)
            {
                case 0:
                    site.SundayOpenTime = null;
                    site.SundayCloseTime = null;
                    break;
                case 1:
                    site.MondayOpenTime = null;
                    site.MondayCloseTime = null;
                    break;
                case 2:
                    site.TuesdayOpenTime = null;
                    site.TuesdayCloseTime = null;
                    break;
                case 3:
                    site.WednesdayOpenTime = null;
                    site.WednesdayCloseTime = null;
                    break;
                case 4:
                    site.ThursdayOpenTime = null;
                    site.ThursdayCloseTime = null;
                    break;
                case 5:
                    site.FridayOpenTime = null;
                    site.FridayCloseTime = null;
                    break;
                case 6:
                    site.SaturdayOpenTime = null;
                    site.SaturdayCloseTime = null;
                    break;
            }

            //update entity and save to db 
            _context.Sites.Update(site);
            var result = await _context.SaveChangesAsync();
            if (result <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            };

            //create post and notify site users
            await _newsFeedService.createAndPostNewsFeedItem(
                $"Trading days/hours have been changed. Make sure to double check your shifts!", site.Id);

            TempData["popUpMessage"] = "Trading Hours Updated!";
            return RedirectToAction("Index");
        }
    }
}
