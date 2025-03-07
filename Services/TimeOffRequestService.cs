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

        public async Task<IEnumerable<TimeOffRequest>> GetAllTimeOffRequestsBySite()
        {
            //gets the current siteId
            var siteId = _siteService.GetSiteIdFromSessionOrUser();

            //Get all the time off requests from users that belong to the appropriate site
            var requests = await _context.TimeOffRequests.Include(t => t.ApplicationUser)
                .Where(t => t.ApplicationUser.SiteId == siteId).ToListAsync();

            var orderedRequests = requests.OrderByDescending(t => t.Date);

            //Refresh the list if any changes are made during runtime
            foreach (var item in requests)
            {
                _context.Entry(item).Reload();
            }

            //Returns list of users
            return orderedRequests;
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
