using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using ProRota.Data;
using ProRota.Models;
using System.Security.Claims;

namespace ProRota.Controllers
{
    public class UserDashboardController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;

        public UserDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            //gets current users ID and then gets the user object
            var userId = _userManager.GetUserId(User);
            var user = _context.ApplicationUsers
                .Where(u => u.Id == userId)
                .Include(u => u.Shifts.OrderByDescending(s => s.StartDateTime)/*.Where(s => s.StartDateTime >= DateTime.Now.Date)*/)
                .Include(u => u.TimeOffRequests)
                .FirstOrDefault();

            ViewBag.HolidaysTaken = user.HolidaysPerYear - user.RemainingHolidays;

            return View(user);
        }

        public async Task<ActionResult> DeleteTimeOffRequest(int id)
        {
            //Check if the ID is null or empty
            if (id == 0)
            {
                return BadRequest();//If ID is null or empty
            }

            //Retrieve the user time off request 
            TimeOffRequest timeOffRequest = await _context.TimeOffRequests.FindAsync(id);

            // Check if the timeOffRequest exists
            if (timeOffRequest == null)
            {
                return NotFound();
            }

            //Pass the user model to the DeactivateUser view
            return View(timeOffRequest);
        }

        public async Task<ActionResult> ConfirmDeleteTimeOffRequest(int requestId)
        {
            //if request id doesnt exist
            if (requestId == 0)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }
        
            //Retrieve the user time off request 
            TimeOffRequest timeOffRequest = await _context.TimeOffRequests.FindAsync(requestId);

            //check to make sure request exists
            if (timeOffRequest == null)
            {
                return NotFound();
            }

            //Get the user by id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return NotFound();
            }

            var user = await _context.ApplicationUsers.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            //if the time off request was a confirmed holiday
            if (timeOffRequest.IsApproved == true && timeOffRequest.IsHoliday == true)
            {
                //incrrment the users remaining holiday
                user.RemainingHolidays++;

                //update the user's holiday information
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update user information.");
                }
            }
          
            //remove the time-off request from the database
            _context.TimeOffRequests.Remove(timeOffRequest);
            var deleteResult = await _context.SaveChangesAsync();

            if (deleteResult <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            }

            //Return to the Index action of ApplicationUser controller
            return RedirectToAction("Index", "ApplicationUser");
        }
    }
}

