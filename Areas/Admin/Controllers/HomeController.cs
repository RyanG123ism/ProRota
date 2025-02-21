using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Services;
using SQLitePCL;
using System.Security.Claims;

namespace ProRota.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Owner")]
    public class HomeController : Controller
    {

        private ApplicationDbContext _context;
        private readonly ICompanyService _companyService;

        public HomeController(ApplicationDbContext context, ICompanyService companySerice) 
        {
            _context = context;
            _companyService = companySerice;
        }
        
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.ApplicationUsers.FindAsync(userId);

            var compnayId = await _companyService.GetCompanyIdFromSessionOrUser();
            var company = await _context.Companies.FindAsync(compnayId) ?? null;
            
            if(company != null)
            {
                //sends the company's sites to view
                var sites = company.Sites.ToList();
                ViewBag.Sites = sites; 
            }

            return View();
        }

        

    }
}
