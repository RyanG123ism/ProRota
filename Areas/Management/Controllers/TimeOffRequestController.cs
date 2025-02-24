using Azure.Core;
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
    public class TimeOffRequestController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ISiteService _siteService;
        private readonly ITimeOffRequestService _timeOffRequestService;

        public TimeOffRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ISiteService siteService, ITimeOffRequestService timeOffRequestService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _siteService = siteService;
            _timeOffRequestService = timeOffRequestService;
        }

        public IActionResult Index(bool isAdmin = false)
        {
            IEnumerable<TimeOffRequest> requests;

            if (isAdmin)
            {
                //checks for existing admin siteId session
                var adminSession = HttpContext.Session.GetInt32("UsersCurrentSite");

                //if session exists then only users from that site will be fetched
                requests = adminSession != null ? _timeOffRequestService.GetAllTimeOffRequestsBySite() : _timeOffRequestService.GetAllTimeOffRequests();
                return View("ViewAllUsers", requests);
            }

            //retrieves all time off requests by site - this is for all roles below admin
            requests = _timeOffRequestService.GetAllTimeOffRequestsBySite();

            //sends tomorrows date to view
            ViewBag.Tomorrow = DateTime.Now.AddDays(1).Date;

            //send temp data from other actions back into the view
            ViewBag.Success = TempData["ConformationMessage"];

            return View("ViewAllTimeOffRequests", requests);
        } 

        public IActionResult SortByRequestStatus(string status)
        {
            //get siteID
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //parse the status string into the approval status enum
            if(Enum.TryParse(status, out ApprovedStatus approvalStatus))
            {
                //retrieve all requests that match that approval status
                var requests = _context.TimeOffRequests.Where(t => t.IsApproved == approvalStatus)
                    .Include(t => t.ApplicationUser).Where(t => t.ApplicationUser.SiteId == siteId).OrderByDescending(t => t.Date).ToList();

                //sends tomorrows date to view
                ViewBag.Tomorrow = DateTime.Now.AddDays(1).Date;

                return View("ViewAllTimeOffRequests", requests);
            }

            // If parsing fails, handle it (e.g., return an error or default view)
            return BadRequest("Invalid status value");
        }

        public async Task<IActionResult> RevertTimeOffRequest(int id)
        {
            var request = await _timeOffRequestService.ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await _timeOffRequestService.ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

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
            var request = await _timeOffRequestService.ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await _timeOffRequestService.ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

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

            if (user.RemainingHolidays <= 0)
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
            var request = await _timeOffRequestService.ValidateTimeOffRequest(id) ?? throw new ArgumentNullException(nameof(id), "Time-off request is invalid.");

            var user = await _timeOffRequestService.ValidateUser(request.ApplicationUserId) ?? throw new ArgumentNullException(nameof(id), "User request is invalid.");

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
    }
}
