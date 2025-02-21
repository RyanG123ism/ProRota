using Bogus.DataSets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Services
{
    public class CompanyService : ICompanyService
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;//accessing httpContext properties of controller base

        public CompanyService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> GetCompanyIdFromSessionOrUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new Exception("HTTP context is not available.");
            }

            //checks for active session
            var companyId = httpContext.Session.GetInt32("UsersCompany");

            if (companyId == null)
            {
                // Get current user's ID and user object
                var userId = _userManager.GetUserId(httpContext.User);
                var user = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);

                // Get the company through the user's site
                var site = await _context.Sites
                    .Include(s => s.ApplicationUsers)
                    .FirstOrDefaultAsync(s => s.ApplicationUsers.Contains(user));

                if (site != null)
                {
                    var company = await _context.Companies
                        .Where(c => c.Id == site.CompanyId)
                        .SingleOrDefaultAsync();

                    if (company != null)
                    {
                        companyId = company.Id;
                    }
                }
                else
                {
                    // If site-based lookup fails, check if the user is the company owner
                    var owner = await _context.Companies.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
                    if (owner != null)
                    {
                        companyId = owner.Id;
                    }
                }
            }
            return (int)companyId;
        }

        public async Task CreateSession()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new Exception("HTTP context is not available.");
            }

            var compnayId = await GetCompanyIdFromSessionOrUser();
            httpContext.Session.SetInt32("UsersCompany", compnayId);
        }
        
    }
}
