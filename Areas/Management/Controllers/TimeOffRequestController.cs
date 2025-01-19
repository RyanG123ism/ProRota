using Azure.Core;
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
    public class TimeOffRequestController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TimeOffRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(bool isAdmin = false)
        {
            IEnumerable<TimeOffRequest> requests;

            if (isAdmin)
            {
                //checks for existing admin siteId session
                var adminSession = HttpContext.Session.GetInt32("AdminsCurrentSiteId");

                //if session exists then only users from that site will be fetched
                requests = adminSession != null ? GetAllTimeOffRequestsBySite() : GetAllTimeOffRequests();
                return View("ViewAllUsers", requests);
            }

            //retrieves all time off requests by site - this is for all roles below admin
            requests = GetAllTimeOffRequestsBySite();

            ViewBag.ConformationMessage = TempData["ConformationMessage"];

            return View("ViewAllTimeOffRequests", requests);
        }

        public IEnumerable<TimeOffRequest> GetAllTimeOffRequests()
        {
            //Get all the requests
            var requests = _context.TimeOffRequests.OrderByDescending(t => t.Date).Include(t => t.ApplicationUser).ToList();

            //Refresh the list if any changes are made during runtime
            foreach (var item in requests)
            {
                _context.Entry(item).Reload();
            }

            //Returns list of users
            return requests;
        }

        public IEnumerable<TimeOffRequest> GetAllTimeOffRequestsBySite()
        {
            //gets the current siteId
            var siteId = GetSiteIdFromSessionOrUser();

            //Get all the time off requests from users that belong to the appropriate site
            var requests = _context.TimeOffRequests.OrderByDescending(t => t.Date).Include(t => t.ApplicationUser)
                .Where(t => t.ApplicationUser.SiteId == siteId);

            //Refresh the list if any changes are made during runtime
            foreach (var item in requests)
            {
                _context.Entry(item).Reload();
            }

            //Returns list of users
            return requests;
        }

        public IActionResult SortByRequestStatus(string status)
        {
            //get siteID
            var siteId = GetSiteIdFromSessionOrUser();

            //parse the status string into the approval status enum
            if(Enum.TryParse(status, out ApprovedStatus approvalStatus))
            {
                //retrieve all requests that match that approval status
                var requests = _context.TimeOffRequests.Where(t => t.IsApproved == approvalStatus)
                    .Include(t => t.ApplicationUser).Where(t => t.ApplicationUser.SiteId == siteId).ToList();

                return View("ViewAllTimeOffRequests", requests);
            }

            // If parsing fails, handle it (e.g., return an error or default view)
            return BadRequest("Invalid status value");
        }

        public async Task<IActionResult> RevertTimeOffRequest(int id)
        {
            var request = await ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

            //reverts the users remaining holiday allowance depending on the status
            if (request.IsApproved == ApprovedStatus.Approved)
            {
                user.RemainingHolidays++;
            }

            //reverts any changes made to status
            request.IsApproved = ApprovedStatus.Pending;

            //updates and saves the DB
            _context.TimeOffRequests.Update(request);
            var result = await _userManager.UpdateAsync(user);

            if(!result.Succeeded)
            {
                throw new Exception("Could not update the User");
            }

            //saves changes
            await _context.SaveChangesAsync();

            //passing conf message through temp data to the next action
            TempData["ConformationMessage"] = $"{user.FirstName}'s Time-Off Request has been Reverted";

            //returns to the time off request index
            return RedirectToAction("Index");

        }

        public async Task<IActionResult> ApproveTimeOffRequest(int id)
        {
            var request = await ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

            //changes the approval status
            request.IsApproved = ApprovedStatus.Approved;

            //takes away a holiday from the user allowance
            if (request.IsHoliday)
            {
                user.RemainingHolidays--;
            }

            //updates and saves the DB
            _context.TimeOffRequests.Update(request);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new Exception("Could not update the User");
            }

            //saves changes
            await _context.SaveChangesAsync();

            //CHANGE THIS BACK TO LESS THAN 0
            if (user.RemainingHolidays >= 0)
            {
                TempData["ConformationMessage"] = $"{user.FirstName} {user.LastName} " +
                    $"has no remaining holidays left in their allowance. If this was a mistake, revert this request.";
            }
            else
            {
                TempData["ConformationMessage"] = $"{user.FirstName}'s Time-Off Request has been Approved";
            }
           
            //returns to the time off request index
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DeclineTimeOffRequest(int id)
        {
            var request = await ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

            //changes the approval status
            request.IsApproved = ApprovedStatus.Denied;

            //updates and saves the DB
            _context.TimeOffRequests.Update(request);
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new Exception("Could not update the User");
            }

            //saves changes
            await _context.SaveChangesAsync();

            TempData["ConformationMessage"] = $"{user.FirstName}'s Time-Off Request has been Denied";

            //returns to the time off request index
            return RedirectToAction("Index");
        }

        
        public async Task<TimeOffRequest?> ValidateTimeOffRequest(int id)
        {
            if (id == 0)
            {
                return null;
            }

            //get time off request
            var request = await _context.TimeOffRequests.FindAsync(id);

            if (request == null)
            {
                return null;
            }

            //returns the time off request
            return request;
        }

        public async Task<ApplicationUser?> ValidateUser(string id)
        {
            if (id == null)
            {
                return null;
            }

            //get time off request
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
            {
                return null;
            }

            //returns the user
            return user;
        }

        public int GetSiteIdFromSessionOrUser()
        {
            //gets admins current session
            var siteId = HttpContext.Session.GetInt32("AdminsCurrentSiteId");

            //if null
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

                //sets user siteId as the site ID
                siteId = user.SiteId;
            }

            return (int)siteId;
        }
    }
}
