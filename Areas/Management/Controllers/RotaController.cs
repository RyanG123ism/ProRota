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
            var rotas = await GeterateWeeklyRotaListForSiteAsync();
            return View(rotas);
        }

        public async Task<ActionResult> ViewWeeklyRota(string weekEnding)
        {
            //Get Site Configuration
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites
                .Where(s => s.Id == siteId)
                .Include(s => s.SiteConfiguration)
                .SingleOrDefaultAsync();

            if (site?.SiteConfiguration == null)
            {
                return BadRequest("Site configuration not found.");
            }

            //Load RoleConfigurations for the SiteConfiguration
            var roleConfigs = await _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == site.SiteConfiguration.Id)
                .Include(rc => rc.RoleCategory)
                    .ThenInclude(rc => rc.Roles) // Load all ApplicationRoles inside RoleCategory
                .ToListAsync();

            //Create a dictionary mapping ApplicationRole.Name -> RoleCategory.Name
            var roleLookup = new Dictionary<string, string>();

            foreach (var roleConfig in roleConfigs)
            {
                if (roleConfig.RoleCategory?.Roles != null)
                {
                    foreach (var role in roleConfig.RoleCategory.Roles)
                    {
                        roleLookup[role.Name] = roleConfig.RoleCategory.Name; // Map Role Name -> Role Category
                    }
                }
            }

            //Fetch Users & Their Identity Roles
            var users = await _context.ApplicationUsers
                .Where(u => u.SiteId == siteId && u.EmailConfirmed)
                .Include(u => u.Shifts) // Fix: Ensure Shifts are loaded
                .Include(u => u.TimeOffRequests) // Fix: Ensure TimeOffRequests are loaded
                .ToListAsync();

            //Fetch User Roles from Identity Tables
            var userRoles = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .Where(ur => users.Select(u => u.Id).Contains(ur.UserId)) //Only fetch roles for relevant users
                .ToDictionaryAsync(ur => ur.UserId, ur => ur.RoleName);

            //Parse the week-ending date
            var weekEndingDate = DateTime.Parse(weekEnding);
            var weekStartingDate = weekEndingDate.AddDays(-6); //Get the week start date

            //Map Users to View Model
            var rota = users
                .Select(u => new ViewRotaViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,

                    // Match User Role to RoleConfig Dictionary
                    Role = userRoles.ContainsKey(u.Id) ? userRoles[u.Id] : "No Role",
                    RoleCategory = userRoles.ContainsKey(u.Id) && roleLookup.ContainsKey(userRoles[u.Id])
                                    ? roleLookup[userRoles[u.Id]]
                                    : "No Category",

                    // Fetch shifts for the selected week
                    Shifts = u.Shifts.Where(s => s.SiteId == siteId &&
                                                 s.StartDateTime >= weekStartingDate &&
                                                 s.StartDateTime <= weekEndingDate.AddHours(23.00)) //adding hours to catch all shifts on the sunday
                                                  .ToList(),

                    // Fetch time-off requests for the selected week
                    TimeOffRequests = u.TimeOffRequests.Where(t => t.Date >= weekStartingDate &&
                                                                   t.Date <= weekEndingDate &&
                                                                   t.IsApproved == ApprovedStatus.Approved).ToList()
                })
                .OrderBy(u => u.RoleCategory) // ✅ Order by Role Category
                .ThenBy(u => u.Role) // (Optional) Order by Role within the category
                .ToDictionary(u => u.Id);

            var rotaSession = HttpContext.Session.GetString("SerializedModel");

            if(rotaSession != null)
            {
                //delete existing session if exists
                HttpContext.Session.Remove("SerializedModel");
            }

            //store serialized rota in session
            HttpContext.Session.SetString("SerializedModel", JsonConvert.SerializeObject(rota, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

            //Pass week-related data to the view
            ViewBag.WeekEndingDate = weekEndingDate;
            ViewBag.WeekStartingDate = weekStartingDate;
            ViewBag.Today = DateTime.Today;

            //Check if any shifts are unpublished
            ViewBag.UnpublishedShifts = rota.Values.Any(r => r.Shifts.Any(s => !s.IsPublished));

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
                        FirstName = g.First().Value.First().ApplicationUser?.FirstName ?? "Unknown",
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
        public async Task<IActionResult> SaveRota(string? serializedModel, bool publishStatus)
        {
            var model = new Dictionary<string, ViewRotaViewModel>();


            if (serializedModel != null)
            {            
                //attempt to extract model from JSON
                model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel);
            }
            else
            {
                //attempt to get the rota from the session
                var rotaSession = HttpContext.Session.GetString("SerializedModel");
                if (rotaSession == null)
                {
                    throw new Exception("Cannot find rota from sesison");
                }
                //extract the serialized model
                model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(rotaSession);
            }

            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            var userIds = model.Keys.ToList();
            var existingShifts = await _context.Shifts
                .Where(s => userIds.Contains(s.ApplicationUserId) && s.SiteId == siteId)
                .ToListAsync();

            if (model.IsNullOrEmpty())
            {
                TempData["PopUp"] = "Error: Could not extract rota data from form";
                return RedirectToAction("Index");
            }

            foreach (var userId in model.Keys)
            {
                foreach (var shift in model[userId].Shifts)
                {
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime == shift.StartDateTime &&
                        s.EndDateTime == shift.EndDateTime &&
                        s.SiteId == siteId);

                    if (existingShift == null)
                    {
                        // If the shift doesn't exist, add it as a new shift
                        shift.ApplicationUserId = userId;
                        shift.SiteId = siteId;
                        shift.IsPublished = publishStatus;
                        _context.Shifts.Add(shift);
                    }
                    else
                    {
                        // If the shift already exists, update it instead
                        existingShift.StartDateTime = shift.StartDateTime;
                        existingShift.EndDateTime = shift.EndDateTime;
                        existingShift.IsPublished = publishStatus;
                        _context.Shifts.Update(existingShift);
                    }
                }
            }

            var result = await _context.SaveChangesAsync();
            if(result < 0)
            {
                ViewBag.Error = "Error: Could not save new shifts to DB";
                return View(model);
            }

            if(publishStatus == true)
            {
                //create post and notify site
                await _newsFeedService.createAndPostNewsFeedItem(
                    $"Trading days/hours have been changed. Make sure to double check your shifts!", siteId);
            }

            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> EditRotaView(bool publishStatus, string weekEnding)
        {
            var serializedModel = HttpContext.Session.GetString("SerializedModel");

            if (string.IsNullOrEmpty(serializedModel))
            {
                TempData["PopUp"] = "Error: Rota Data missing from JSON";
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


            //list of userID's of users that have had their shifts edited
            List<string> affectedUserIds = editedShiftsDict.Keys.ToList();

            //list of all shift dates from the editedShifts dictionary
            var shiftDates = editedShiftsDict.Values
                .SelectMany(s => s.Keys)
                .Distinct()
                .Select(DateTime.Parse)
                .ToList();

            //queries the db for shifts that match the usersId and shift dates
            var existingShifts = await _context.Shifts
                .Where(s => affectedUserIds.Contains(s.ApplicationUserId) &&
                            s.StartDateTime.HasValue &&
                            shiftDates.Contains(s.StartDateTime.Value.Date) &&
                            s.SiteId == siteId)
                .ToListAsync();

            //placeholders for shifts to add, update, and delete
            List<Shift> shiftsToAdd = new List<Shift>();
            List<Shift> shiftsToUpdate = new List<Shift>();
            List<Shift> shiftsToDelete = new List<Shift>();

            //loops through each users edited shifts
            foreach (var userEntry in editedShiftsDict)
            {
                string userId = userEntry.Key;//gets the user Id

                //loop through each shift 
                foreach (var shiftEntry in userEntry.Value)
                {
                    //get the new date and times for the shift
                    DateTime shiftDate = DateTime.Parse(shiftEntry.Key);
                    ShiftUpdateModel newShiftData = shiftEntry.Value;

                    //look for an existing shift
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime.HasValue &&
                        s.StartDateTime.Value.Date == shiftDate);

                    //if times are null - the shift was removed during editing
                    if (newShiftData.StartTime == null || newShiftData.EndTime == null)
                    {
                        //add to DELETE list
                        if (existingShift != null)
                        {
                            shiftsToDelete.Add(existingShift);
                        }
                    }
                    else
                    {
                        //get the start / end times
                        DateTime newStart = shiftDate.Date.Add(TimeSpan.Parse(newShiftData.StartTime));
                        DateTime newEnd = shiftDate.Date.Add(TimeSpan.Parse(newShiftData.EndTime));

                        //if no current shift exists then add to the ADD list 
                        if (existingShift == null)
                        {
                            shiftsToAdd.Add(new Shift
                            {
                                ApplicationUserId = userId,
                                SiteId = siteId,
                                StartDateTime = newStart,
                                EndDateTime = newEnd,
                                IsPublished = publishStatus // assigning publish status (from view)
                            });
                        }
                        else if (existingShift.StartDateTime.Value != newStart || existingShift.EndDateTime.Value != newEnd)
                        {
                            //if shift exists then add to UPDATE list
                            existingShift.StartDateTime = newStart;
                            existingShift.EndDateTime = newEnd;
                            shiftsToUpdate.Add(existingShift);
                        }
                    }
                }
            }

            //update the changes to the DB 

            if (shiftsToDelete.Any()) _context.Shifts.RemoveRange(shiftsToDelete);
            if (shiftsToAdd.Any()) await _context.Shifts.AddRangeAsync(shiftsToAdd);
            await _context.SaveChangesAsync();

            if (shiftsToUpdate.Any())
            {
                _context.Shifts.UpdateRange(shiftsToUpdate);
                await _context.SaveChangesAsync();
            }

            //return to the view rota page
            return RedirectToAction("ViewWeeklyRota", new { weekEnding = weekEndingDate });

        }

        [HttpPost]
        public async Task<IActionResult> UnpublishRota(string? serializedModel)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var model = new Dictionary<string, ViewRotaViewModel>();

            if (serializedModel != null)
            {
                //attempt to extract model from JSON
                model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(serializedModel);
            }
            else
            {
                //attempt to get the rota from the session
                var rotaSession = HttpContext.Session.GetString("SerializedModel");
                if (rotaSession == null)
                {
                    throw new Exception("Cannot find rota from session");
                }
                //extract the serialized model
                model = JsonConvert.DeserializeObject<Dictionary<string, ViewRotaViewModel>>(rotaSession);
            }

            if (model == null || !model.Any())
            {
                TempData["PopUp"] = "Error: Could not extract rota data from form";
                return RedirectToAction("Index");
            }

            //get all existing shifts for the users in the rota
            var userIds = model.Keys.ToList();
            var existingShifts = await _context.Shifts
                .Where(s => userIds.Contains(s.ApplicationUserId) && s.SiteId == siteId)
                .ToListAsync();

            List<Shift> shiftsToUpdate = new List<Shift>();

            foreach (var userId in model.Keys)
            {
                foreach (var shift in model[userId].Shifts)
                {
                    // Find existing shift in the DB
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime == shift.StartDateTime &&
                        s.EndDateTime == shift.EndDateTime &&
                        s.SiteId == siteId);

                    if (existingShift != null)
                    {
                        existingShift.IsPublished = false; // Unpublish shift
                        shiftsToUpdate.Add(existingShift);
                    }
                }
            }

            if (shiftsToUpdate.Any())
            {
                _context.Shifts.UpdateRange(shiftsToUpdate);
                await _context.SaveChangesAsync();
            }

            // Notify the site about the change
            await _newsFeedService.createAndPostNewsFeedItem(
                $"The rota has been unpublished. Please review your shifts.", siteId);

            return RedirectToAction("Index");
        }


        private async Task<IActionResult> ReloadOnlyAffectedUsers(List<string> affectedUserIds, int siteId)
        {
            var users = await _context.ApplicationUsers
                .Where(u => affectedUserIds.Contains(u.Id))
                .ToListAsync();

            var userRoles = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .Where(ur => affectedUserIds.Contains(ur.UserId))
                .ToDictionaryAsync(ur => ur.UserId, ur => ur.RoleName);

            DateTime weekEndingDate = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek);
            DateTime weekStartingDate = weekEndingDate.AddDays(-6);

            var rota = users
                .Select(u => new ViewRotaViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = userRoles.ContainsKey(u.Id) ? userRoles[u.Id] : "No Role",
                    RoleCategory = "Category here", // You may adjust based on how role categories are stored
                    Shifts = _context.Shifts
                        .Where(s => s.ApplicationUserId == u.Id &&
                                    s.SiteId == siteId &&
                                    s.StartDateTime.HasValue &&
                                    s.StartDateTime.Value >= weekStartingDate &&
                                    s.StartDateTime.Value <= weekEndingDate &&
                                    s.IsPublished)
                        .ToList(),
                    TimeOffRequests = _context.TimeOffRequests
                        .Where(t => t.ApplicationUserId == u.Id &&
                                    t.Date >= weekStartingDate &&
                                    t.Date <= weekEndingDate &&
                                    t.IsApproved == ApprovedStatus.Approved)
                        .ToList()
                })
                .ToDictionary(u => u.Id);

            ViewBag.WeekStartingDate = weekStartingDate;
            ViewBag.WeekEndingDate = weekEndingDate;
            ViewBag.Today = DateTime.Today;
            ViewBag.Editable = weekEndingDate >= DateTime.Now;
            ViewBag.UnpublishedShifts = rota.Values.Any(r => r.Shifts.Any(s => !s.IsPublished));

            return View("ViewWeeklyRota", rota);
        }





    }
}
