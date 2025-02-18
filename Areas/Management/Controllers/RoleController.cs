using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Migrations;
using ProRota.Models;
using ProRota.Services;
using SQLitePCL;
using System.Security.Policy;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Admin, General Manager, Assistant Manager, Head Chef, Executive Chef, Operations Manager")]
    [Area("Management")]
    public class RoleController : Controller
    {

        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            if (siteId == null)
            {
                throw new Exception("Site Id not found");
            }

            //get all roles and role categories that belong to a site
            var rolesAndRoleCategories = _context.SiteConfigurations.Where(s => s.SiteId == siteId)
                .Include(s => s.RoleConfigurations)
                    .ThenInclude(rc => rc.RoleCategory)
                    .ThenInclude(rc => rc.Roles)
                    .SingleOrDefault();

            return View(rolesAndRoleCategories);
        }

        [HttpGet]
        public async Task<IActionResult> CreateRoleConfiguration()
        {
            var viewModel = new CreateRoleConfigurationViewModel();
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> CreateRoleConfiguration(
            [Bind(include: "MinEmployees, MaxEmployees, SelectedRoleCategory")] CreateRoleConfigurationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Return the view with validation errors
            }

            if(model.MinEmployees > model.MaxEmployees)
            {
                //send error message to view
                ViewBag.ErrorMessage = "Minimum Employee count must be less than Maximum Employee count";
                return View(model);
            }

            //placeholder
            RoleCategory roleCategory;

            //getting the site configuraiton
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? null;

            if(siteConfig == null)
            {
                siteConfig = new SiteConfiguration
                {
                    SiteId = siteId,
                };

                _context.SiteConfigurations.Add(siteConfig);
                await _context.SaveChangesAsync();
            }

            //get all role configs relatingt to this site
            var roleConfigs = await _context.RoleConfigurations.Where(r => r.SiteConfigurationId == siteConfig.Id).Include(r => r.RoleCategory).ToListAsync();

            //attempt to retrieve a category with the same name - if one exists
            var existingRoleCategory = roleConfigs.Where(r => r.RoleCategory.Name == model.SelectedRoleCategory).SingleOrDefault();

            if (existingRoleCategory != null)
            {
                ViewBag.ErrorMessage = $"Role Configuration already exissts with the category name: {model.SelectedRoleCategory}";
                return View(model);
            }
            else
            {
                //create a new role category
                roleCategory = new RoleCategory
                {
                    Name = model.SelectedRoleCategory,
                    Roles = new List<ApplicationRole>()
                };

                //save role category
                _context.RoleCategories.Add(roleCategory);
                await _context.SaveChangesAsync();
            }

            //create new role config
            var roleConfig = new RoleConfiguration
            {
                SiteConfigurationId = siteConfig.Id,
                MaxEmployees = model.MaxEmployees,
                MinEmployees = model.MinEmployees,
                RoleCategoryId = roleCategory.Id
            };

            _context.RoleConfigurations.Add(roleConfig);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> CreateRole()
        {
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            //getting all role configs in site config
            var roleConfigs = await _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id) 
                .Include(rc => rc.RoleCategory)
                .Distinct() //Avoid duplicate roles
                .ToListAsync();

            var viewModel = new CreateRoleViewModel
            {
                RoleCategories = roleConfigs.Select(rc => new SelectListItem
                {
                    Value = rc.RoleCategory.Id.ToString(),
                    Text = rc.RoleCategory.Name
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(
            [Bind(include: "RoleName, SelectedRoleCategoryId")] CreateRoleViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            //getting all roles in site config
            var roles = await _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                .SelectMany(rc => rc.RoleCategory.Roles)
                .Distinct()
                .ToListAsync();

            if(roles.Any(r => r.Name == model.RoleName)) 
            {
                ViewBag.PopUpMessage = "Role already exists";
                return View(model);
            }

            //create role
            var identityRole = new ApplicationRole
            {
                Name = model.RoleName,
                RoleCategoryId = Convert.ToInt32(model.SelectedRoleCategoryId)
            };
            await _roleManager.CreateAsync(identityRole);
            await _context.SaveChangesAsync();


            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditRoleConfiguration(int id)
        {
            if (id == 0)
            {
                throw new Exception("Role ID cannot be found");
            }

            //get role configuration with the category object
            var roleConfig = _context.RoleConfigurations
                .Include(rc => rc.RoleCategory)
                .SingleOrDefault(rc => rc.Id == id)
                ?? throw new Exception("Cannot find role configuration");

            var model = new EditRoleConfigurationViewModel
            {
                Id = roleConfig.Id,
                MaxEmployees = roleConfig.MaxEmployees,
                MinEmployees = roleConfig.MinEmployees,
                SelectedRoleCategory = roleConfig.RoleCategory.Name
            };

            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> EditRoleConfiguration(
            [Bind(include:"Id, MaxEmployees, MinEmployees, SelectedRoleCategory")]EditRoleConfigurationViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            if(model.MinEmployees > model.MaxEmployees)
            {
                //send error message to view
                ViewBag.ErrorMessage = "Minimum Employee count must be less than Maximum Employee count";
                return View(model);
            }

            //getting the site configuraiton
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            //get role configuration with the category object
            var roleConfig = await _context.RoleConfigurations
                .Include(rc => rc.RoleCategory)
                .SingleOrDefaultAsync(rc => rc.Id == model.Id)
                ?? throw new Exception("Cannot find role configuration");

            //check to see if existing role category exists with that name - excluding the current role category
            var existingRoleCategory = await _context.RoleConfigurations
                .Where(r => r.SiteConfigurationId == siteConfig.Id
                            && r.RoleCategory.Name == model.SelectedRoleCategory
                            && r.Id != roleConfig.Id) //exclude current roleConfig from check
                .Select(r => r.RoleCategory)
                .FirstOrDefaultAsync();

            //if a category with that name already exists
            if (existingRoleCategory != null)
            {
                ViewBag.ErrorMessage = $"Role Configuration already exissts with the category name: {model.SelectedRoleCategory}";
                return View(model);
            }

            //update the role category information
            var roleCategory = _context.RoleCategories.Find(roleConfig.RoleCategoryId);
            if (roleCategory == null) 
            { 
                throw new Exception("Cannot find role category"); 
            }

            //if the name has changed - update the role category
            if (roleCategory.Name != model.SelectedRoleCategory)
            {
                roleCategory.Name = model.SelectedRoleCategory;
                _context.RoleCategories.Update(roleCategory);
                await _context.SaveChangesAsync();
            }

            //update role config info
            roleConfig.MaxEmployees = model.MaxEmployees;
            roleConfig.MinEmployees = model.MinEmployees;

            _context.RoleConfigurations.Update(roleConfig);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if(id == null)
            {
                throw new Exception("Cannot find role Id");
            }

            //get the role
            var role = await _context.Roles.Where(r => r.Id == id).SingleOrDefaultAsync() ?? null;

            if(role == null) 
            {
                throw new Exception("Cannot find role object");
            }

            //get the site config
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            var roleConfigs = await _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfig.Id)
                .Include(rc => rc.RoleCategory)
                .Distinct() //Avoid duplicate roles
                .ToListAsync();

            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name,
                RoleCategories = roleConfigs.Select(rc => new SelectListItem
                {
                    Value = rc.RoleCategory.Id.ToString(),
                    Text = rc.RoleCategory.Name
                }).ToList()
            };

            return View(model);


        }

        [HttpPost]
        public async Task<IActionResult> EditRole(
            [Bind(include: "Id, RoleName, SelectedRoleCategoryId")]EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //get role
            var role = _context.Roles.Find(model.Id) ?? null;

            //get the site
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var siteConfig = _context.SiteConfigurations.SingleOrDefault(s => s.SiteId == siteId) ?? throw new Exception("Cannot find Site Configuration");

            if (role == null)
            {
                throw new Exception("Cannot find role object");
            }

            //if role caetgory was changed
            if(role.RoleCategoryId != model.SelectedRoleCategoryId)
            {
                role.RoleCategoryId = model.SelectedRoleCategoryId;
            }

            //if name was chanegd
            if(role.Name != model.RoleName)
            {
                //checks for and existing role
                var existingRole = await _context.RoleConfigurations
                    .Where(rc => rc.SiteConfigurationId == siteConfig.Id) 
                    .SelectMany(rc => rc.RoleCategory.Roles) //flatten the roles
                    .Where(r => r.Name == model.RoleName) //find matching role name
                    .FirstOrDefaultAsync(); 

                if (existingRole != null)
                {
                    ViewBag.ErrorMessage = $"{model.RoleName} already exists for this site";
                    return View(model);
                }
        
                role.Name = model.RoleName;
            }

            await _roleManager.UpdateAsync(role);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}


