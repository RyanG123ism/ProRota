using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;
using System.Collections;
using System.Security.Claims;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef, Operations Manager")]
    [Area("Management")]
    public class RotaController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;

        public RotaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager /*RoleManager<IdentityRole> roleManager*/)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {              
            //generates rota list
            var rotas = await GeterateWeeklyRotaListForSiteAsync();
            return View(rotas);    
        }

        public async Task<ActionResult> ViewWeeklyRota(string weekEnding)
        {        
            //checks wether siteID is of user or the admins session (if admin is logged in)
            var siteId = GetSiteIdFromSessionOrUser();

            var weekEndingDate = DateTime.Parse(weekEnding);
            var weekStartingDate = weekEndingDate.AddDays(-7); // Week start date from the end date
            
            //passing true to view only if the rota is of this week or in the future
            if(weekEndingDate >= DateTime.Now)
            {
                ViewBag.Editable = true;
            }

            // Get all users and include the shifts and time off requests within the given date parameters
            var rota = await _context.ApplicationUsers
                .Include(u => u.Shifts.Where(s => s.SiteId == siteId).Where(s => s.StartDateTime >= weekStartingDate && s.StartDateTime <= weekEndingDate).Where(s => s.IsPublished == true))
                .Include(u => u.TimeOffRequests.Where(t => t.Date >= weekStartingDate && t.Date <= weekEndingDate).Where(t => t.IsApproved == ApprovedStatus.Approved))
                .Where(u => u.SiteId == siteId)
                .ToListAsync();

            ViewBag.WeekStartingDate = weekStartingDate;//passing starting date to view to help format dates

            return View(rota);
        }

        private async Task<Dictionary<string, Dictionary<string, List<Shift>>>> GeterateWeeklyRotaListForSiteAsync()
        {
            var siteId = GetSiteIdFromSessionOrUser();

            //get shifts for the managers corresponding site for the last 12 weeks (84 days)
            var shifts = _context.Shifts.ToList().Where(s => s.SiteId == siteId).Where(s => s.StartDateTime > DateTime.Now.AddDays(-84));

            //dictionary for all weekly rotas
            var weeklyRotas = new Dictionary<string, List<Shift>>();

            //dictionary to hold multiple dicitonaries of catagorised weekly rotas 
            var categorisedWeeklyRotas = new Dictionary<string, Dictionary<string, List<Shift>>>
            {
                { "Unpublished", new Dictionary<string, List<Shift>>() },
                { "ActiveWeek", new Dictionary<string, List<Shift>>() },
                { "Published", new Dictionary<string, List<Shift>>() }
            };

            foreach (var shift in shifts)
            {
                //gets the next sunday date
                var weekEnding = CalculateNextSundayDateToString(shift);

                //if key doesnt exist then add it along with the shift value in a new list
                if(!weeklyRotas.ContainsKey(weekEnding))
                {
                    weeklyRotas[weekEnding] = new List<Shift>
                    {
                        shift
                    };
                }
                //add shift to existing key and list value
                weeklyRotas[weekEnding].Add(shift);                           
            }

            //orders the dictionary by decending date key values
            weeklyRotas.OrderByDescending(r => DateTime.Parse(r.Key)).ToDictionary(r => r.Key, r => r.Value);

            //determine the active week (this week ending Sunday)
            var activeWeekKey = CalculateNextSundayDateToString(DateTime.Now);

            //loop through each weekly rota and categorise it
            foreach (var entry in weeklyRotas)
            {
                string weekEnding = entry.Key;
                List<Shift> shiftsInWeek = entry.Value;

                //check if any shift in this week is unpublished
                bool hasUnpublishedShifts = shiftsInWeek.Any(s => !s.IsPublished);

                if (weekEnding == activeWeekKey)
                {
                    //set as active week
                    categorisedWeeklyRotas["ActiveWeek"][weekEnding] = shiftsInWeek;
                }
                else if (hasUnpublishedShifts)
                {
                    //add to Unpublished if at least one shift is not published
                    categorisedWeeklyRotas["Unpublished"][weekEnding] = shiftsInWeek;
                }
                else
                {
                    //otherwise, add to Published
                    categorisedWeeklyRotas["Published"][weekEnding] = shiftsInWeek;
                }
            }

            //sending todays date to view for comparrison later
            ViewBag.Today = DateTime.Today;

            return categorisedWeeklyRotas;
        }

        public async Task<IActionResult> CreateNewRota()
        {
            var siteId = GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);

            if(site == null)
            {
                throw new Exception("Site Not Found");
            }

            //tracking the day of week
            var week = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            //dictionary to store open days
            var openDays = new Dictionary<string, bool>();

            //loops through day of week and tried to find open and closing times for the site
            foreach (var day in week)
            {
                var openTime = typeof(Site).GetProperty($"{day}OpenTime")?.GetValue(site) as DateTime?;
                var closeTime = typeof(Site).GetProperty($"{day}CloseTime")?.GetValue(site) as DateTime?;
                openDays[day] = openTime != null && closeTime != null; //returns true if both times exist
            }

            //send data to the view
            ViewBag.Week = week;
            ViewBag.OpenDays = openDays;

            return View(site);
        }



        private string CalculateNextSundayDateToString(Shift shift)
        {
            return CalculateNextSundayDateToString(shift.StartDateTime.Value);
        }

        private string CalculateNextSundayDateToString(DateTime date)
        {
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            var endOfWeekDate = date.AddDays(daysUntilSunday);

            return endOfWeekDate.ToString("yyyy-MM-dd");
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
