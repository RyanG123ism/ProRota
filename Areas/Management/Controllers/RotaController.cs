using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
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

            //passing true to view only if the rota is of this week or in the future
            if (weekEndingDate >= DateTime.Now)
            {
                ViewBag.Editable = true;
            }

            //getting all users that belong to the site along with their shifts and time off requests
            var rota = await _context.ApplicationUsers
                .Where(u => u.SiteId == siteId)
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

            ViewBag.WeekStartingDate = weekStartingDate;//passing starting date to view to help format dates

            return View(rota);
        }

        private async Task<Dictionary<string, Dictionary<string, List<Shift>>>> GeterateWeeklyRotaListForSiteAsync()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

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
            weeklyRotas.OrderByDescending(r => DateTime.Parse(r.Key)).ToDictionary(r => r.Key, r => r.Value);

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
                var openTime = typeof(Site).GetProperty($"{day}OpenTime")?.GetValue(site) as DateTime?;
                var closeTime = typeof(Site).GetProperty($"{day}CloseTime")?.GetValue(site) as DateTime?;
                openDays[day] = openTime != null && closeTime != null; //returns true if both times exist
            }

            //send data to the view
            ViewBag.Week = week;
            ViewBag.OpenDays = openDays;

            return View(site);
        }

        [HttpPost]
        public IActionResult CreateNewRota(CreateWeeklyRotaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //get the site
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = _context.Sites.Find(siteId);

            if(site == null)
            {
                throw new Exception("Cannot find Site Object");
            }

            //passing the view model along with the site object to the alogrithm to create a new rota
            //var newRota = _algorithmService.CreateWeeklyRota(model, site);


            return RedirectToAction("Index");
        }

    }
}
