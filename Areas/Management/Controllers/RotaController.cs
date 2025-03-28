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
        private readonly INewsFeedService _newsFeedService;

        public RotaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, IRotaService rotaService, IAlgorithmService algorithmService, INewsFeedService newsFeedService)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _rotaService = rotaService;
            _algorithmService = algorithmService;
            _newsFeedService = newsFeedService;
        }

        public async Task<IActionResult> Index()
        {
            //generates rota list
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var rotas = await _rotaService.GeterateWeeklyRotaListForSiteAsync(siteId);

            //sending todays date to view for comparrison later
            ViewBag.Today = DateTime.Today;

            //picking up any pop ups
            if(TempData["Error"] != null) ViewBag.Error = TempData["Error"];
            if(TempData["Success"] != null) ViewBag.Error = TempData["Success"];
            if(TempData["Alert"] != null) ViewBag.Error = TempData["Alert"];

            return View(rotas);
        }

        public async Task<ActionResult> ViewWeeklyRota(string weekEnding)
        {
            //Get Site Configuration
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //get week ending date
            var weekEndingDate = DateTime.Parse(weekEnding);

            //get the rota for the week
            var rota = await _rotaService.GetWeeklyRota(siteId, weekEndingDate);

            var rotaSession = HttpContext.Session.GetString("SerializedModel");

            //delete existing session if exists
            if (rotaSession != null) HttpContext.Session.Remove("SerializedModel");

            //store serialized rota in session
            HttpContext.Session.SetString("SerializedModel", JsonConvert.SerializeObject(rota, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

            //Pass week-related data to the view
            ViewBag.WeekEndingDate = weekEndingDate;
            ViewBag.WeekStartingDate = weekEndingDate.AddDays(-6);
            ViewBag.Today = DateTime.Today;
            //Check if any shifts are unpublished
            ViewBag.UnpublishedShifts = rota.Values.Any(r => r.Shifts.Any(s => !s.IsPublished));

            return View(rota);
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
                TempData["Error"] = $"Something went wrong while creating your rota - redirected back to a safe location";
                return RedirectToAction("Index");
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

            //look for existing shifts 
            var existingRota = await _context.Shifts.Where(s => s.SiteId == siteId 
                        && s.StartDateTime >= model.WeekEndingDate.AddDays(-6) 
                        && s.EndDateTime <= model.WeekEndingDate).AnyAsync();

            if(existingRota)
            {
                TempData["Error"] = $"Shifts already exist for the week ending {model.WeekEndingDate:dd/MM/yyyy}. Cannot create a duplicate rota";
                return RedirectToAction("Index");
            }

            //passing the view model along with the site object to the alogrithm to create a new rota
            var rota = await _algorithmService.CreateWeeklyRota(model, site);
            var timeOffRequests = await _algorithmService.MapTimeOffRequests(model.WeekEndingDate, site.Id);

            var viewModel = await _rotaService.CreateRotaViewModel(rota, timeOffRequests, siteId, site.SiteConfiguration.Id);

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
        public async Task<IActionResult> SaveRota(string? serializedModel, bool publishStatus)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            var model = serializedModel != null
                ? JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel)
                : JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(HttpContext.Session.GetString("SerializedModel"));

            if (model == null)
            {
                TempData["Error"] = "Error: Could not extract rota data from form";
                return RedirectToAction("Index");
            }

            var success = await _rotaService.SaveRota(model, siteId, publishStatus);

            if (!success)
            {
                TempData["Error"] = "Error: Could not save shifts to DB";
                return RedirectToAction("Index");
            }

            if(publishStatus == true)
            {
                //create post and notify site
                await _newsFeedService.createAndPostNewsFeedItem(
                    $"Shifts have been updated. Make sure to check your rota!", siteId);
            }

            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> EditRotaView(bool publishStatus, string weekEnding)
        {
            var serializedModel = HttpContext.Session.GetString("SerializedModel");

            if (string.IsNullOrEmpty(serializedModel))
            {
                TempData["Error"] = "Error: Rota Data missing from JSON";
                return RedirectToAction("Index");
            }

            var model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel);

            if (model == null)
            {
                throw new Exception("Error deserializing JSON data");
            }

            var weekEndingDate = !string.IsNullOrEmpty(weekEnding) ? DateTime.Parse(weekEnding) : DateTime.Today;

            ViewBag.WeekEndingDate = weekEndingDate;
            ViewBag.WeekStartingDate = weekEndingDate.AddDays(-6);

            ViewBag.Today = DateTime.Today;
            ViewBag.UnpublishedShifts = publishStatus ? false : true;

            return View("EditRota", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRota(bool publishStatus, string weekEndingDate, string editedShifts)
        {
            if (string.IsNullOrEmpty(editedShifts))
            {
                TempData["Error"] = "No shifts to update.";
                return RedirectToAction("Index");
            }

            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            var editedShiftsDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, ShiftUpdateModel>>>(editedShifts);

            var success = await _rotaService.EditRota(editedShiftsDict, siteId, publishStatus);

            if (!success)
            {
                TempData["Error"] = "Error Updating Rota";
                return RedirectToAction("Index");
            }

            //return to the view rota page
            return RedirectToAction("ViewWeeklyRota", new { weekEnding = weekEndingDate });

        }

        [HttpPost]
        public async Task<IActionResult> UnpublishRota(string? serializedModel)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            var model = serializedModel != null
                ? JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel)
                : JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(HttpContext.Session.GetString("SerializedModel"));

            if (model == null)
            {
                TempData["Error"] = "Error: Could not extract rota data from form";
                return RedirectToAction("Index");
            }

            var success = await _rotaService.UnpublishRota(model, siteId);

            if(!success)
            {
                TempData["Error"] = "Error Unpublishing Rota - Please note that ProRota is unable to unpublish shifts within the current week. If you need to remove shifts, use the Edit function";
                return RedirectToAction("Index");
            }

            // Notify the site about the change
            await _newsFeedService.createAndPostNewsFeedItem(
                $"The rota has been unpublished while we review some things. Please keep a look out for your shifts updating.", siteId);

            return RedirectToAction("Index");
        }
    }
}
