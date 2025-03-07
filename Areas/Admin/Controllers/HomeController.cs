using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;
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
        private readonly IClaimsService _claimsService;

        public HomeController(ApplicationDbContext context, IClaimsService claimsService) 
        {
            _context = context;
            _claimsService = claimsService;
        }
        
        public async Task<IActionResult> Index()
        {
            var companyId = _claimsService.GetCompanyId();
            var company = await _context.Companies.FindAsync(companyId) ?? null;
            
            if(company != null)
            {
                //sends the company and sites to view
                var sites = company.Sites.ToList();
                ViewBag.Sites = await RefreshSites(companyId);
            }

            return View(company);
        }

        public async Task<IEnumerable<Site>> RefreshSites(int id)
        {
            if(id == 0)
            {
                return Enumerable.Empty<Site>();
            }

            var sites = await _context.Sites.Where(s => s.CompanyId == id).ToListAsync();

            return sites;
        }

        

    }
}
