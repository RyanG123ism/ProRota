using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.Models;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using System.Security.Claims;
using System.Security.Policy;
using System.Text.Encodings.Web;
using System.Text;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Owner, Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class ApplicationUserController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ISiteService _siteService;
        private readonly ICompanyService _companyService;
        private readonly IExtendedEmailSender _emailSender;
        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ISiteService siteService, ICompanyService companyService, IExtendedEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _siteService = siteService;
            _companyService = companyService;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(bool isAdmin = false)//default value is false for when no admins access index page
        {

            IEnumerable<ApplicationUser> users;

            //if link is accessed by admin or owner
            if (isAdmin)
            {
                //checks for existing admin siteId session
                var currentSession = HttpContext.Session.GetInt32("UsersCurrentSite");

                //if session exists then only users from that site will be fetched ELSE all company users will be returned
                users = currentSession != null ? await ViewAllUsersBySite() : await ViewAllUsersByCompany();

                return View("ViewAllUsers", users);

            }

            //Calls the ViewAllUsers method to retrieve the list of users
            users = await ViewAllUsersBySite();

            if (TempData["PopUp"] != null)
            {
                ViewBag.Success = TempData["PopUp"];
            }

            return View("ViewAllUsers", users);
       
        }

        public async Task<IEnumerable<ApplicationUser>> ViewAllUsersByCompany()
        {
            var companyId = await _companyService.GetCompanyIdFromSessionOrUser();

            if (companyId == null)
            {
                throw new Exception("Cannot retrieve company ID from session");
            }
            var company = await _context.Companies.Where(c => c.Id == companyId).ToListAsync();

            var users = await _context.Companies
                .Where(c => c.Id == companyId)
                .SelectMany(c => c.Sites)
                .SelectMany(s => s.ApplicationUsers.Where(u => u.EmailConfirmed == true))
                .ToListAsync();

            var invitedUsers = _context.Companies
                .Where(c => c.Id == companyId)
                .SelectMany(c => c.Sites)
                .SelectMany(s => s.ApplicationUsers.Where(u => u.EmailConfirmed == false))
                .ToListAsync();

            ViewBag.InvitedUsers = invitedUsers; //user that havent confirmed their account yet

            if (TempData["PopUp"] != null)
            {
                ViewBag.Alert = TempData["PopUp"];
            }
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return users;
        }


        public async Task<IEnumerable<ApplicationUser>> ViewAllUsersBySite()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Get all the users
            var users = await _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == true).ToListAsync();
            var invitedUsers = await _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == false).ToListAsync();

            //user that havent confirmed their account yet
            ViewBag.InvitedUsers = invitedUsers; 

            //Pass all the roles to the view so that you can search users by role
            ViewBag.Roles = await _context.Roles.ToListAsync();

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
                var usersFirstNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == true).Where(u => u.FirstName.Equals(fName)).ToList();

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
                var usersFirstNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == true).Where(u => u.FirstName.Equals(fName)).ToList();
                //Retrieve users with matching last names
                var usersLastNames = _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == true).Where(u => u.LastName.Equals(fName)).ToList();

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
            var usersInRoleBySite = usersInRole.Where(u => u.SiteId == siteId && u.EmailConfirmed == true).ToList();

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
            var site = await _context.Sites.FindAsync(siteId);
            //getting siteConfig - for assigning roles
            var siteConfig = await _context.SiteConfigurations.SingleOrDefaultAsync(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            if (ModelState.IsValid)
            {
                // Validate email uniqueness before creating the user
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    //If the existing user was a previosuly deactivated user, or has an account but it isn't linked to a company - then just edit their account rather than creating a new user
                    if(await _userManager.IsInRoleAsync(existingUser, "Deactivated") || await _userManager.IsInRoleAsync(existingUser, "Partial_User_Unpaid"))
                    {

                        existingUser.FirstName = model.FirstName;
                        existingUser.LastName = model.LastName;
                        existingUser.Email = model.Email;
                        existingUser.Salary = model.Salary;
                        existingUser.ContractualHours = model.ContractualHours;
                        existingUser.Notes =model.Notes;

                        await _userManager.RemoveFromRoleAsync(existingUser, "Deactivated");
                        await _userManager.AddToRoleAsync(existingUser, model.Role);

                        await _userManager.UpdateAsync(existingUser);
                        await _context.SaveChangesAsync();

                        // Get the existing claims for the user
                        var claims = await _userManager.GetClaimsAsync(existingUser);

                        // Find the existing SiteId claim
                        var siteClaim = claims.FirstOrDefault(c => c.Type == "SiteId");

                        if (siteClaim != null && int.TryParse(siteClaim.Value, out int siteClaimId))
                        {
                            if (siteClaimId != siteId) // Only update if SiteId has changed
                            {
                                // Remove old SiteId claim (pass Claim object, not string)
                                await _userManager.RemoveClaimAsync(existingUser, siteClaim);

                                // Add new SiteId claim
                                await _userManager.AddClaimAsync(existingUser, new Claim("SiteId", siteId.ToString()));
                            }
                        }

                        // Update the user model
                        existingUser.SiteId = siteId;

                        TempData["PopUp"] = $"{existingUser.FirstName} {existingUser.LastName}'s account has been reinstated. You can now resend their invite email";

                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Email is already in use.");
                    }
                    
                    return RedirectToAction("Index");
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
                ApplicationUser user = new ApplicationUser()
                {
                    Email = model.Email,
                    UserName = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Salary = model.Salary,
                    ContractualHours = model.ContractualHours,
                    Notes = model.Notes,
                    EmailConfirmed = false,
                    SiteId= siteId
                };

                //Creates the user in the DB and adds the password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    //add to role
                    var userId = await _userManager.GetUserIdAsync(user);
                    await _userManager.AddToRoleAsync(user, model.Role);
                    
                    //send email invite here
                    //generate email conformation token
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: "ConfirmEmail",
                        values: new { area = "Identity", userId = userId, code = code, invited = true, companyId = site.CompanyId},
                    protocol: Request.Scheme);

                    var emailBody = _emailSender.CreateInviteEmailBody(user, model.Role, site.SiteName, callbackUrl);

                    await _emailSender.SendEmailAsync(user.Email, "You're Invited to Join ProRota!", emailBody);

                    TempData["PopUp"] = "User Account Created and Invitation Sent!";
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

        public async Task<IActionResult> ResendInvite(string id)
        {
            //get user info
            var user = await _userManager.FindByIdAsync(id);
            var site = _context.Sites.Where(s => s.Id == user.SiteId).SingleOrDefault();
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.SingleOrDefault();

            //if user is deactivated then return to index
            if(roleName == "Deactivated")
            {
                TempData["PopUp"] = $"{user.FirstName} {user.LastName} is currently deactivated and sending an invite will not work - if you would like to bring this user back, use the 'Create User' button and use the same email address.";
                return RedirectToAction("Index", "ApplicationUser");
            }

            //send email invite here
            //generate email conformation token
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: "ConfirmEmail",
                values: new { area = "Identity", userId = user.Id, code = code, invited = true },
            protocol: Request.Scheme);

            var emailBody = _emailSender.CreateInviteEmailBody(user, roleName, site.SiteName, callbackUrl);

            await _emailSender.SendEmailAsync(user.Email, "You're Invited to Join ProRota!", emailBody);

            TempData["PopUp"] = $"Invitation Re-sent to {user.FirstName}!";
            return RedirectToAction("Index", "ApplicationUser");
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
