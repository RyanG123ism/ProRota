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

            // Get all users and include the shifts and time off requests within the given date parameters
            var rota = await _context.ApplicationUsers
                .Include(u => u.Shifts.Where(s => s.SiteId == siteId).Where(s => s.StartDateTime >= weekStartingDate && s.StartDateTime <= weekEndingDate))
                .Include(u => u.TimeOffRequests.Where(t => t.Date >= weekStartingDate && t.Date <= weekEndingDate).Where(t => t.IsApproved == ApprovedStatus.Approved))
                .Where(u => u.SiteId == siteId)
                .ToListAsync();

            ViewBag.WeekStartingDate = weekStartingDate;//passing starting date to view to help format dates

            return View(rota);
        }

        private async Task<Dictionary<string, List<Shift>>> GeterateWeeklyRotaListForSiteAsync()
        {
            var siteId = GetSiteIdFromSessionOrUser();

            //get shifts for the managers corresponding site for the last 12 weeks (84 days)
            var shifts = _context.Shifts.ToList().Where(s => s.SiteId == siteId).Where(s => s.StartDateTime > DateTime.Now.AddDays(-84));

            var weeklyRotas = new Dictionary<string, List<Shift>>();

            foreach(var  shift in shifts)
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
            var sortedWeeklyRotas = weeklyRotas.OrderByDescending(r => DateTime.Parse(r.Key)).ToDictionary(r => r.Key, r => r.Value);

            return sortedWeeklyRotas;
        }

        private string CalculateNextSundayDateToString(Shift shift)
        {
            var shiftDate = shift.StartDateTime.Value;

            // adds seven to dayofWeek enum then calculates the remainder
            // using % 7 operator so that if shift is on a sunday (7) then it divides by 7 and gives us 0
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)shiftDate.DayOfWeek + 7) % 7;
            var endOfWeekDate = shiftDate.AddDays(daysUntilSunday);

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
