using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Migrations;
using ProRota.Models;
using ProRota.Services;
using System.Collections;
using System.Security.Claims;
using System.Security.Policy;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Owner, Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class RotaController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly IRotaService _rotaService;
        private readonly IAlgorithmService _algorithmService;

        public RotaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, IRotaService rotaService, IAlgorithmService algorithmService)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _rotaService = rotaService;
            _algorithmService = algorithmService;
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
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            var weekEndingDate = DateTime.Parse(weekEnding);
            var weekStartingDate = weekEndingDate.AddDays(-7); // Week start date from the end date

            //querying all users of the site
            //formatting data into a dictionary of the ViewRotaViewModel
            var rota = await _context.ApplicationUsers
                .Where(u => u.SiteId == siteId && u.EmailConfirmed == true)
                .Select(u => new ViewRotaViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Shifts = u.Shifts.Where(s => s.SiteId == siteId &&
                                                 s.StartDateTime >= weekStartingDate &&
                                                 s.StartDateTime <= weekEndingDate &&
                                                 s.IsPublished == true).ToList(),
                    TimeOffRequests = u.TimeOffRequests.Where(t => t.Date >= weekStartingDate &&
                                                                   t.Date <= weekEndingDate &&
                                                                   t.IsApproved == ApprovedStatus.Approved).ToList()
                })
                .ToDictionaryAsync(u => u.Id);

            //passing starting and ending and today date to view to help format dates
            ViewBag.WeekStartingDate = weekStartingDate;
            ViewBag.WeekEndingDate = weekEndingDate;
            ViewBag.Today = DateTime.Today;

            //passing true to view only if the rota is of this week or in the future
            if (weekEndingDate >= DateTime.Now)
            {
                ViewBag.Editable = true;
            }

            //passing the publish status of the rota to the view
            bool UnpublishedShifts = rota.Values.Any(r => r.Shifts.Any(s => !s.IsPublished));
            ViewBag.UnpublishedShifts = UnpublishedShifts;

            


            return View(rota);
        }

        public async Task<Dictionary<string, Dictionary<string, List<Shift>>>> GeterateWeeklyRotaListForSiteAsync()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //get shifts for the managers corresponding site for the last 12 weeks (84 days)
            var shifts = _context.Shifts.ToList().Where(s => s.SiteId == siteId).Where(s => s.StartDateTime > DateTime.Now.AddDays(-84));

            //dictionary for all weekly rotas
            var weeklyRotas = new Dictionary<string, List<Shift>>();

            //dictionary to hold multiple dicitonaries of catagorised weekly rotas 
            var categorisedWeeklyRotas = new Dictionary<string, Dictionary<string, List<Shift>>>
            {
                { "Unpublished Rotas", new Dictionary<string, List<Shift>>() },
                { "Current Week", new Dictionary<string, List<Shift>>() },
                { "Published Rotas", new Dictionary<string, List<Shift>>() }
            };

            foreach (var shift in shifts)
            {
                //gets the next sunday date
                var weekEnding = _rotaService.CalculateNextSundayDateToString(shift);

                //if key doesnt exist then add it along with the shift value in a new list
                if (!weeklyRotas.ContainsKey(weekEnding))
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
            weeklyRotas = weeklyRotas.OrderByDescending(r => DateTime.Parse(r.Key)).ToDictionary(r => r.Key, r => r.Value);

            //determine the active week (this week ending Sunday)
            var activeWeekKey = _rotaService.CalculateNextSundayDateToString(DateTime.Now);

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
                    categorisedWeeklyRotas["Current Week"][weekEnding] = shiftsInWeek;
                }
                else if (hasUnpublishedShifts)
                {
                    //add to Unpublished if at least one shift is not published
                    categorisedWeeklyRotas["Unpublished Rotas"][weekEnding] = shiftsInWeek;
                }
                else
                {
                    //otherwise, add to Published
                    categorisedWeeklyRotas["Published Rotas"][weekEnding] = shiftsInWeek;
                }
            }

            //sending todays date to view for comparrison later
            ViewBag.Today = DateTime.Today;

            return categorisedWeeklyRotas;
        }

        [HttpGet]
        public async Task<IActionResult> CreateNewRota()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);

            if (site == null)
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
                var openTime = typeof(ProRota.Models.Site).GetProperty($"{day}OpenTime")?.GetValue(site) as DateTime?;
                var closeTime = typeof(ProRota.Models.Site).GetProperty($"{day}CloseTime")?.GetValue(site) as DateTime?;
                openDays[day] = openTime != null && closeTime != null; //returns true if both times exist
            }

            //send data to the view
            ViewBag.Week = week;
            ViewBag.OpenDays = openDays;

            return View(site);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewRota(CreateWeeklyRotaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //get the site with the site configuration and role objects attached
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.Where(s => s.Id == siteId)
                .Include(s => s.SiteConfiguration)
                .ThenInclude(sc => sc.RoleConfigurations)
                .ThenInclude(rc => rc.RoleCategory)
                .ThenInclude(rc => rc.Roles)
                .FirstOrDefaultAsync();

            if(site == null)
            {
                throw new Exception("Cannot find Site Object");
            }

            //passing the view model along with the site object to the alogrithm to create a new rota
            var rota = await _algorithmService.CreateWeeklyRota(model, site);
            var timeOffRequests = await _algorithmService.MapTimeOffRequests(model.WeekEndingDate, site.Id);

            var allShifts = rota.SelectMany(r => r.Value).SelectMany(kvp => kvp.Value).ToList();

            //find the earliest shift date to pass to the view model as the week starting date
            DateTime? earliestShiftDate = allShifts.Any() ? allShifts.Min(s => s.StartDateTime?.Date)  : null;
            ViewBag.WeekStartingDate = earliestShiftDate;

            //convert rota into a dictionary where the key is the User ID and value is a ViewRotaViewModel
            var viewModel = rota
                .SelectMany(r => r.Value) //flatten the dictionary
                .GroupBy(r => r.Key) //group shifts by UserId
                .ToDictionary(
                    g => g.Key, 
                    g => new ViewRotaViewModel
                    {
                        Id = g.Key,
                        FirstName = g.First().Value.First().ApplicationUser?.FirstName ?? "Unknown", // Get first shift's user
                        LastName = g.First().Value.First().ApplicationUser?.LastName ?? "Unknown",
                        Shifts = g.SelectMany(x => x.Value).ToList(), // Flatten all shifts into a single list
                        TimeOffRequests = timeOffRequests.ContainsKey(g.Key) ? timeOffRequests[g.Key] : new List<TimeOffRequest>()
                    });

            //user keys
            var assignedUserIds = new HashSet<string>(viewModel.Keys);

            //users that didnt get assigned any shifts
            var unasignedUsers = await _context.ApplicationUsers.Where(u => u.SiteId == site.Id && u.EmailConfirmed == true && !assignedUserIds.Contains(u.Id)).Include(u => u.TimeOffRequests.Where(t => t.IsApproved == ApprovedStatus.Approved)).ToListAsync() ?? null;
            

            if (unasignedUsers != null || unasignedUsers.Count > 0 ) 
            {
                //adding unasigned users to the viewModel
                foreach (var user in unasignedUsers)
                {
                    viewModel.Add(user.Id, new ViewRotaViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Shifts = new List<Shift>(),
                        TimeOffRequests = user.TimeOffRequests.ToList()
                    }); ;
                }
            }

            //lets the view know that this is the first instance of this rota, its not been saved yet
            ViewBag.InitialCreate = true;

            //passing starting and ending and today date to view to help format dates
            ViewBag.WeekStartingDate = model.WeekEndingDate.AddDays(-6);
            ViewBag.WeekEndingDate = model.WeekEndingDate;
            ViewBag.Today = DateTime.Today;

            //passing true to view only if the rota is of this week or in the future
            if (model.WeekEndingDate >= DateTime.Now)
            {
                ViewBag.Editable = true;
            }

            //passing the publish status of the rota to the view
            ViewBag.UnpublishedShifts = true;

            return View("ViewWeeklyRota", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRota(string serializedModel, bool publishStatus)
        {
            //if no data is passed in
            if (string.IsNullOrEmpty(serializedModel))
            {
                TempData["PopUp"] = "Error: Rota Data missing from JSON";
                return RedirectToAction("Index");
            }

            //extract the serialized model
            var model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel);
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            if(model.IsNullOrEmpty())
            {
                TempData["PopUp"] = "Error: Could not extract rota data from form";
                return RedirectToAction("Index");
            }

            foreach (var userId in model.Keys)
            {
                foreach (var shift in model[userId].Shifts)
                {
                    shift.ApplicationUserId = userId;
                    shift.SiteId = siteId;
                    shift.IsPublished = publishStatus;//changing published status
                    _context.Shifts.Add(shift);//adding to db
                }
            }

            var result = await _context.SaveChangesAsync();
            if(result < 0)
            {
                ViewBag.Error = "Error: Could not save new shifts to DB";
                return View(model);
            }

            return RedirectToAction("Index");

        }

        

    }
}
