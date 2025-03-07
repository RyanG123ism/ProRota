using Microsoft.AspNetCore.Identity;
using ProRota.Models;
using System.Security.Claims;

namespace ProRota.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ClaimsService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            //returns the logged in users Id
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public int GetCompanyId()
        {
            //returns the logged in users companyId claim
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out int companyId) ? companyId : 0;
        }

        public int GetSiteId()
        {
            //returns the site Id of the current logged in user
            var siteIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("SiteId")?.Value;
            return int.TryParse(siteIdClaim, out int siteId) ? siteId : 0;
        }

        public async Task<bool> SetCompanyId(string userId, int companyId)
        {
            if (userId == null) return false;
            if (companyId == 0) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            //get users claim
            var claimsIdentity = (ClaimsIdentity)_httpContextAccessor.HttpContext.User.Identity;
            var existingClaim = claimsIdentity.FindFirst("CompanyId");
            
            if (existingClaim != null)
            {
                //first remove any old claim then add new company ID
                claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("CompanyId"));     
            }

            claimsIdentity.AddClaim(new Claim("CompanyId", companyId.ToString()));

            return true;
        }

        public async Task<bool> SetSiteId()
        {
            var userId = GetUserId();
            if (userId == null) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.SiteId == 0) return false;

            //get users claim 
            var claimsIdentity = (ClaimsIdentity)_httpContextAccessor.HttpContext.User.Identity;
            var existingClaim = claimsIdentity.FindFirst("SiteId");

            if (existingClaim != null)
            {
                //remove any old claim and add new siteId
                claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("SiteId"));
            }

            claimsIdentity.AddClaim(new Claim("SiteId", user.SiteId.ToString()));

            return true;
        }

        public async Task<bool> SetSiteId(string userId)
        {
            if (userId == null) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.SiteId == 0) return false;

            //get users claim 
            var claimsIdentity = (ClaimsIdentity)_httpContextAccessor.HttpContext.User.Identity;
            var existingClaim = claimsIdentity.FindFirst("SiteId");

            if (existingClaim != null)
            {
                //remove any old claim and add new siteId
                claimsIdentity.RemoveClaim(claimsIdentity.FindFirst("SiteId"));
            }

            claimsIdentity.AddClaim(new Claim("SiteId", user.SiteId.ToString()));

            return true;
        }
    }
}
