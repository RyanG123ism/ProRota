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
