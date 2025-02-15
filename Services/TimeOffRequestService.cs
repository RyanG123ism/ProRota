using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Services
{
    public class TimeOffRequestService : ITimeOffRequestService
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly IHttpContextAccessor _httpContextAccessor;//accessing httpContext properties of controller base

        public TimeOffRequestService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _httpContextAccessor = httpContextAccessor;
            
        }

        public IEnumerable<TimeOffRequest> GetAllTimeOffRequests()
        {
            //Get all the requests
            var requests = _context.TimeOffRequests.OrderByDescending(t => t.Date).Include(t => t.ApplicationUser).ToList();

            //Refresh the list if any changes are made during runtime
            foreach (var item in requests)
            {
                _context.Entry(item).Reload();
            }

            //Returns list of users
            return requests;
        }

        public IEnumerable<TimeOffRequest> GetAllTimeOffRequestsBySite()
        {
            //gets the current siteId
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Get all the time off requests from users that belong to the appropriate site
            var requests = _context.TimeOffRequests.OrderByDescending(t => t.Date).Include(t => t.ApplicationUser)
                .Where(t => t.ApplicationUser.SiteId == siteId);

            //Refresh the list if any changes are made during runtime
            foreach (var item in requests)
            {
                _context.Entry(item).Reload();
            }

            //Returns list of users
            return requests;
        }

        public async Task<TimeOffRequest?> ValidateTimeOffRequest(int id)
        {
            if (id == 0)
            {
                return null;
            }

            //get time off request
            var request = await _context.TimeOffRequests.FindAsync(id);

            if (request == null)
            {
                return null;
            }

            //returns the time off request
            return request;
        }

        public async Task<ApplicationUser?> ValidateUser(string id)
        {
            if (id == null)
            {
                return null;
            }

            //get time off request
            var user = await _context.ApplicationUsers.FindAsync(id);

            if (user == null)
            {
                return null;
            }

            //returns the user
            return user;
        }

    }
}
