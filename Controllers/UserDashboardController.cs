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
            var user = getUserInfo();
            ViewBag.HolidaysTaken = user.HolidaysPerYear - user.RemainingHolidays;
            ViewBag.Today = DateTime.Now.Date;
            return View(user);
        }

        public ApplicationUser getUserInfo()
        {
            //gets current users ID and then gets the user object
            var userId = _userManager.GetUserId(User);
            var user = _context.ApplicationUsers
                .Where(u => u.Id == userId)                                     //MAKE SURE TO INCLUDE THIS 
                .Include(u => u.Shifts.OrderByDescending(s => s.StartDateTime)/*.Where(s => s.StartDateTime >= DateTime.Now.Date)*/)
                .Include(u => u.TimeOffRequests.OrderByDescending(t => t.Date))
                .Include(u => u.TimeOffRequests)
                .FirstOrDefault();

            //reloads the current db instance to include shifts and requests added at runtime
            if (user != null)
            {
                _context.Entry(user).Collection(u => u.TimeOffRequests).Load();
                _context.Entry(user).Collection(u => u.Shifts).Load();

                return user;
            }

            throw new Exception("User object not found");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTimeOffRequest(DateTime requestDate, string requestNotes, bool isPaidHoliday)
        {
            //find user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(userId == null)
            {
                return NotFound();
            }

            //gets todays date
            var today = DateTime.Today.Date;

            //if date is null or a date in the past
            if (requestDate == DateTime.MinValue || requestDate <= today)
            {
                //sends an error message to the view
                var errorMessage = $"Request date must be a date after {today.ToShortDateString()}";
                ViewBag.ErrorMessage = errorMessage;
            }

            //checking for null
            if(requestNotes == null)
            {
                requestNotes = "";
            }
            //create time off request
            var timeOffRequest = new TimeOffRequest
            {
                Date = requestDate,
                Notes = requestNotes,
                IsHoliday = isPaidHoliday,
                IsApproved = ApprovedStatus.Pending, //pending as default
                ApplicationUserId = userId
            };

            //add to db and save
            await _context.TimeOffRequests.AddAsync(timeOffRequest);
            var result = await _context.SaveChangesAsync();

            if (result <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save changes to the database.");
            }

            //returning to the userdashboard
            return RedirectToAction("Index");
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
            if (timeOffRequest.IsApproved == ApprovedStatus.Approved && timeOffRequest.IsHoliday == true)
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
            return RedirectToAction("Index");
        }
    }
}

