using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using Stripe.Billing;
using System.Security.Claims;

namespace ProRota.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Owner")]
    public class SiteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompanyService _companyService;


        public SiteController(ApplicationDbContext context, ICompanyService companyService)
        {
            _context = context;
            _companyService = companyService;
        }

        public async Task<IActionResult> CreateSite(string siteName)
        {
            if(siteName.IsNullOrEmpty())
            {
                throw new Exception("Cannot find siteName");
            }

            //getting company Id
            var companyId = await _companyService.GetCompanyIdFromSessionOrUser();

            //looking for existing sites within the company 
            var existingSite = _context.Sites.Where(s => s.CompanyId == companyId && s.SiteName == siteName).FirstOrDefault() ?? null;

            if (existingSite != null)
            {
                TempData["AlertMessage"] = $"Error: {siteName} already exists. Choose another name";
            }

            var site = new Site
            {
                SiteName = siteName,
                CompanyId = companyId
            };

            await _context.Sites.AddAsync(site);
            await _context.SaveChangesAsync();

            return View();
        }
    }
}
