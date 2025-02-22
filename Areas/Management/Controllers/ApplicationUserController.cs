﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.Models;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using System.Security.Claims;
using System.Security.Policy;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef, Operations Manager")]
    [Area("Management")]
    public class ApplicationUserController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ISiteService _siteService;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ISiteService siteService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _siteService = siteService;
        }

        public IActionResult Index(bool isAdmin = false)//default value is false for when no admins access index page
        {

            IEnumerable<ApplicationUser> users;

            if (isAdmin)
            {
                //checks for existing admin siteId session
                var adminSession = HttpContext.Session.GetInt32("AdminsCurrentSiteId");

                //if session exists then only users from that site will be fetched
                users = adminSession != null ? ViewAllUsersBySite() : ViewAllUsers();

                return View("ViewAllUsers", users);

            }

            //Calls the ViewAllUsers method to retrieve the list of users
            users = ViewAllUsersBySite();

            return View("ViewAllUsers", users);
       
        }
        
        public IEnumerable<ApplicationUser> ViewAllUsers()
        {    
            //Get all the users
            var users = _context.ApplicationUsers.ToList();

            //Pass all the roles to the view so that you can search users by role
            ViewBag.Roles = _context.Roles.ToList();

            //Returns list of users
            return users;
        }

        public IEnumerable<ApplicationUser> ViewAllUsersBySite()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Get all the users
            var users = _context.ApplicationUsers.Where(u => u.SiteId == siteId).ToList();

            //Pass all the roles to the view so that you can search users by role
            ViewBag.Roles = _context.Roles.ToList();

            //Returns list of users
            return users;
        }

        public ActionResult SearchForUser(string fullName)
        {

            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Check if the fullName parameter is null or empty
            if (string.IsNullOrEmpty(fullName))
            {
                return View("SearchUserError"); //If fullName is null or empty, return a view indicating an error
            }

            //Passing all the roles to the view so that you can search users by role
            ViewBag.Roles = _context.Roles.ToList();
            string fName;
            string lName;

            //If the search term contains a space - then we know its 2 names the user is searching for
            if (fullName.Contains(" "))
            {
                var names = fullName.Split(' ');
                fName = names[0];
                lName = names[1];

                //Retrieve users with matching first names
                var usersFirstNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId).Where(u => u.FirstName.Equals(fName)).ToList();

                List<ApplicationUser> results = new List<ApplicationUser>();

                //Iterate through users with matching first names and filter by last name
                foreach (var item in usersFirstNames)
                {
                    if (item.LastName.Equals(lName))
                    {
                        results.Add(item);
                    }
                }
                return View("ViewAllUsers", results); //Return the ViewAllUsers view with filtered results
            }
            else//Searching for first names AND last names seperately
            {
                fName = fullName;
                lName = null;

                //Retrieve users with matching first names
                var usersFirstNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId).Where(u => u.FirstName.Equals(fName)).ToList();
                //Retrieve users with matching last names
                var usersLastNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId).Where(u => u.LastName.Equals(fName)).ToList();

                //Joins the list of first names and last name search results together
                var results = usersFirstNames.Concat(usersLastNames);

                return View("ViewAllUsers", results); //Return the ViewAllUsers view with filtered results
            }

        }

        public async Task<ActionResult> ViewAllUsersByRole(string id)
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Find the role via the Id
            var role = await _context.Roles.FindAsync(id);
            var roleName = role.Name;

            //Get users in the specified role then filters to the specific site
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var usersInRoleBySite = usersInRole.Where(u => u.SiteId == siteId).ToList();

            //Passing users in the specified role to the view
            ViewBag.Roles = await _context.Roles.ToListAsync(); // Ensure this operation is awaited

            if (usersInRole == null || usersInRole.Any() || usersInRole.Count == 0)
            {
                ViewBag.ErrorMessage = $"There are no users belonging to the role {roleName} ";
            }

            return View("ViewAllUsers", usersInRoleBySite);
        }

        [HttpGet]
        public ActionResult CreateApplicationUser()
        {
            //getting siteConfig - for assigning roles
            var siteId = _siteService.GetSiteIdFromSessionOrUser();    
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            //get roles from via the roleConfig within a sites siteConfig
            var roles = _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                .SelectMany(rc => rc.RoleCategory.Roles) 
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name 
                })
                .ToList();

            //creating instance of ViewModel
            CreateApplicationUserViewModel user = new CreateApplicationUserViewModel
            {
                Roles = roles
            };

            if (!roles.Any())
            {
                ViewBag.ErrorMessage = "No roles available for this site.";
            }

            //Return the user model to the view
            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> CreateApplicationUser(
            [Bind(include: "Email,Password,FirstName,LastName, Salary, ContractualHours, Notes, Role")] CreateApplicationUserViewModel model)
        {
            //getting the siteId from the current user - to bind to user created
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //getting siteConfig - for assigning roles
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            if (ModelState.IsValid)
            {
                // Validate email uniqueness before creating the user
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email is already in use.");
                    return View(model);
                }

                //check for existing role within this site
                var role = await _context.RoleConfigurations
                    .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                    .SelectMany(rc => rc.RoleCategory.Roles)
                    .FirstOrDefaultAsync(r => r.Name == model.Role);

                if (role == null)
                {
                    ModelState.AddModelError(string.Empty, "The selected role is not available for this site.");
                    return View(model);
                }

                //find the role config linked to the role
                var roleConfig = await _context.RoleConfigurations
                    .Where(rc => rc.SiteConfigurationId == siteConfig.Id
                                 && rc.RoleCategory.Roles.Any(r => r.Name == model.Role))//making sure that the role category belong to the correct site config
                    .FirstOrDefaultAsync();

                if (roleConfig == null)
                {
                    ModelState.AddModelError(string.Empty, "Role Configuration not found.");
                    return View(model);
                }

                //Creates a new user and populates data via the model input
                ApplicationUser u = new ApplicationUser()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Salary = model.Salary,
                    ContractualHours = model.ContractualHours,
                    Notes = model.Notes,
                    EmailConfirmed = true, // this is so the user can login - IF I have time will change to 2fa
                    SiteId= siteId
                };

                //Creates the user in the DB and adds the password
                var result = await _userManager.CreateAsync(u, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(u, model.Role);

                    TempData["SuccessMessage"] = "User Account Created!";

                    return RedirectToAction("Index", "ApplicationUser");
                }
                else
                {
                    //Handle errors if user creation fails
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            //Reload roles and assign them to the model before returning to the view
            var roles = _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                .SelectMany(rc => rc.RoleCategory.Roles)
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                })
                .ToList();
            model.Roles = roles;

            //If ModelState is invalid or if something goes wrong, return to the view with the model
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> EditApplicationUser(string id)
        {
            //Check if the ID is null or empty
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(); //If ID is null or empty, return a BadRequest response
            }

            //getting siteConfig - for assigning roles
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            //Retrieve the user using UserManager
            ApplicationUser user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(); //If user is not found, return a NotFound response
            }


            //Get the user's roles and extracts the name 
            var role = await _userManager.GetRolesAsync(user);
            string roleName = role.FirstOrDefault() ?? "No Role Assigned";

            //get roles from via the roleConfig within a sites siteConfig
            var roles = _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                .SelectMany(rc => rc.RoleCategory.Roles)
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                })
                .ToList();

            return View(new EditApplicationUserViewModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName, 
                Salary = user.Salary,
                ContractualHours = user.ContractualHours,
                Notes = user.Notes,
                CurrentRole = roleName,
                Roles = roles
            });
        }

        [HttpPost]
        public async Task<ActionResult> EditApplicationUser(string id,
            [Bind(include: "Email,FirstName,LastName, Salary, ContractualHours, Notes, CurrentRole, Role")] EditApplicationUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                //Retrieve the user using UserManager
                ApplicationUser user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(); //If user is not found, return a NotFound response
                }

                //Updating user details to reflect the model
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Salary = model.Salary;
                user.ContractualHours = model.ContractualHours;
                user.Notes = model.Notes;

                //checking if the role was changed during edit
                if(model.CurrentRole != model.Role)
                {
                    //Remove the user from the current role
                    await _userManager.RemoveFromRoleAsync(user, model.CurrentRole);
                    //Add the user to the new role
                    await _userManager.AddToRoleAsync(user, model.Role);

                }

                //Update the user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    //Redirect to the appropriate action
                    return RedirectToAction("Index", "ApplicationUser");
                }
                else
                {
                    //Handle errors if updating user fails
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            //If ModelState is invalid or if something goes wrong, return to the view with the model
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            ApplicationUser user = _context.ApplicationUsers.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            // Get the roles of the user
            var roles = await _userManager.GetRolesAsync(user);

            // Pass the first role of the user to the view
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(user);
        }

        public async Task<ActionResult> DeactivateUser(string id)
        {
            //Check if the ID is null or empty
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(); //If ID is null or empty, return a BadRequest response
            }

            //Retrieve the user by id
            ApplicationUser user = await _context.ApplicationUsers.FindAsync(id); //If user is not found, return a NotFound response

            // Check if the user exists
            if (user == null)
            {
                return NotFound();
            }

            //Pass the user model to the DeactivateUser view
            return View(user);
        }

        public async Task<ActionResult> ConfirmDeactivateUser(string id)
        {
            if (id == null)
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }

            //Get the user by id
            ApplicationUser user = await _context.ApplicationUsers.FindAsync(id);

            //Return to index if user tries to delete their own account
            if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return RedirectToAction("Index", "ApplicationUser");
            }

            if (user == null)
            {
                return NotFound();
            }

            //Get roles of the user
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any())
            {
                //Remove user from all roles
                foreach (var role in roles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            //Add user to the "Deactivated" role
            await _userManager.AddToRoleAsync(user, "Deactivated");

            //Log the user deactivation action
            var deactivatedUser = await _context.ApplicationUsers.FindAsync(id);

            //Return to the Index action of ApplicationUser controller
            return RedirectToAction("Index", "ApplicationUser");
        }
    }

}
