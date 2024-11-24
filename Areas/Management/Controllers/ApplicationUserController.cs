using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class ApplicationUserController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            //Calls the ViewAllUsers method to retrieve the list of users
            var users = ViewAllUsers();

            // Returns the ViewAllUsers view with the list of users
            return View("ViewAllUsers", users);
        }

        public IEnumerable<ApplicationUser> ViewAllUsers()
        {
            //Get all the users
            var users = _context.ApplicationUsers.ToList();

            //Refresh the list if any changes are made
            foreach (var item in users)
            {
                _context.Entry(item).Reload();
            }

            //Pass all the roles to the view so that you can search users by role
            ViewBag.Roles = _context.Roles.ToList();

            //Returns list of users
            return users;
        }




    }
}
