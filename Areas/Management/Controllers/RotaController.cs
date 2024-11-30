using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProRota.Data;
using ProRota.Models;
using System.Collections;
using System.Security.Claims;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class RotaController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        //private readonly RoleManager<IdentityRole> _roleManager;

        public RotaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager /*RoleManager<IdentityRole> roleManager*/)
        {
            _context = context;
            _userManager = userManager;
            //_roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            //Calls the ViewAllUsers method to retrieve the list of users
            var rotas = await GeterateWeeklyRotaListForSiteAsync();

            // Returns the ViewAllUsers view with the list of users
            return View(rotas);
        }

        public void getWeekEndings()
        {

        }




        //DONT NEED ANY OF THIS -------------------------------------------------------------------------
        private async Task<Dictionary<string, List<Shift>>> GeterateWeeklyRotaListForSiteAsync()
        {
            //get user
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.ApplicationUsers.FindAsync(id);

            //get shifts for the managers corresponding site for the last 12 weeks (84 days)
            var shifts = _context.Shifts.ToList().Where(s => s.SiteId == user.SiteId).Where(s => s.StartDateTime > DateTime.Now.AddDays(-84));

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

            return weeklyRotas;
        }

        private string CalculateNextSundayDateToString(Shift shift)
        {
            var shiftDate = shift.StartDateTime.Value;

            // adss seven to dayofWeek enum then calculates the remainder
            // using % 7 operator so that if shift is on a sunday (7) then it divides by 7 and gives us 0
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)shiftDate.DayOfWeek + 7) % 7;
            var endOfWeekDate = shiftDate.AddDays(daysUntilSunday);

            return endOfWeekDate.ToString("yyyy-MM-dd");

        }
    }
}
