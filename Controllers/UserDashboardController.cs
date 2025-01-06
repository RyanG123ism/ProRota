using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }
}
