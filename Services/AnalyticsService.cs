using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ISiteService _siteService;
        private readonly IRotaService _rotaService;


        public AnalyticsService(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, ISiteService siteService, IRotaService rotaService)
        {
            _context = context;
            _roleManager = roleManager;
            _siteService = siteService;
            _rotaService = rotaService;

        }

        public async Task<Dictionary<string, int>> GetTotalHoursBarChartValues(Site site, DateTime weekStartingDate, DateTime weekEndingDate)
        {
            //get all published shifts for the site within the selected week
            var shifts = await _context.Shifts
                .Where(s => s.SiteId == site.Id &&
                            s.StartDateTime.HasValue &&
                            s.EndDateTime.HasValue &&
                            s.StartDateTime.Value.Date >= weekStartingDate &&
                            s.EndDateTime.Value.Date <= weekEndingDate &&
                            s.IsPublished)
                .ToListAsync();

            //initialise the dictionary with 0 hours for each day of the week
            var dailyHours = new Dictionary<string, int>();

            for (int i = 0; i < 7; i++)
            {
                //adding each day of the week as the key 
                var day = weekStartingDate.AddDays(i).DayOfWeek.ToString();
                dailyHours[day] = 0;
            }

            //group shifts by StartDate and sum the total hours per day
            foreach (var shift in shifts)
            {
                var day = shift.StartDateTime.Value.DayOfWeek.ToString();

                var duration = (shift.EndDateTime.Value - shift.StartDateTime.Value).TotalHours;
                dailyHours[day] += (int)Math.Round(duration);
            }

            return dailyHours;
        }

        public async Task<Dictionary<string, int>> GetRoleDistributionPieChartValues(Site site,List<ApplicationUser> users,DateTime weekStart,DateTime weekEnd)
        {
            //extract user Id's
            var userIds = users.Select(u => u.Id).ToList();

            //get all shifts within the week that belong to the users
            var shifts = await _context.Shifts
                .Where(s => s.SiteId == site.Id &&
                            s.ApplicationUserId != null &&
                            userIds.Contains(s.ApplicationUserId) &&
                            s.StartDateTime >= weekStart &&
                            s.EndDateTime <= weekEnd &&
                            s.IsPublished)
                .ToListAsync();
            
            //get the users role
            var userRoles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            //assign the users to dictionary with user ID as the key and role as the value
            var userRoleDict = userRoles.ToDictionary(r => r.UserId, r => r.RoleName);

            var roleShiftCounts = shifts
                .Where(s => userRoleDict.ContainsKey(s.ApplicationUserId))
                .GroupBy(s => userRoleDict[s.ApplicationUserId])
                .ToDictionary(g => g.Key, g => g.Count());

            return roleShiftCounts;
        }

        public async Task<Dictionary<string, decimal>> GetWageDataLineGraphValues(Site site,Dictionary<string, int> totalHoursByDay,DateTime weekStart,DateTime weekEnd)
        {
            // 1. Get all users for the site
            var users = await _context.ApplicationUsers
                .Where(u => u.SiteId == site.Id && u.EmailConfirmed)
                .ToListAsync();

            // 2. Build a userId -> hourlyWage dictionary
            var wageDict = users.ToDictionary(
                u => u.Id,
                u => u.Salary
            );

            // 3. Get all published shifts in the week
            var shifts = await _context.Shifts
                .Where(s => s.SiteId == site.Id &&
                            s.IsPublished &&
                            s.StartDateTime >= weekStart &&
                            s.EndDateTime <= weekEnd &&
                            s.ApplicationUserId != null)
                .ToListAsync();

            // 4. Create expenditure tracker
            var wageByDay = new Dictionary<string, decimal>();

            foreach (var day in totalHoursByDay.Keys)
            {
                wageByDay[day] = 0m;
            }

            // 5. Aggregate wages per shift per day
            foreach (var shift in shifts)
            {
                var day = shift.StartDateTime.Value.DayOfWeek.ToString();
                var userId = shift.ApplicationUserId;

                if (!wageDict.ContainsKey(userId)) continue;

                var wage = (decimal)wageDict[userId];
                var hours = (decimal)(shift.EndDateTime.Value - shift.StartDateTime.Value).TotalHours;

                wageByDay[day] += Math.Round(wage * hours, 2);
            }

            return wageByDay;
        }


        public async Task<int[,]> GetShiftTimeHeatmapDataValues(Site site, DateTime weekStart, DateTime weekEnd)
        {
            // Heatmap: [hour, dayOfWeek] → Monday = 0, Sunday = 6
            int[,] heatmap = new int[24, 7];

            var shifts = await _context.Shifts
                .Where(s => s.SiteId == site.Id &&
                            s.IsPublished &&
                            s.StartDateTime.HasValue &&
                            s.EndDateTime.HasValue &&
                            s.StartDateTime >= weekStart &&
                            s.EndDateTime <= weekEnd)
                .ToListAsync();

            foreach (var shift in shifts)
            {
                var start = shift.StartDateTime.Value;
                var end = shift.EndDateTime.Value;

                // Loop through each hour the shift spans
                for (var dt = start; dt < end; dt = dt.AddHours(1))
                {
                    int hour = dt.Hour;
                    int dayIndex = ((int)dt.DayOfWeek + 6) % 7; // make Monday = 0 ... Sunday = 6

                    heatmap[hour, dayIndex]++;
                }
            }

            return heatmap;
        }
    }
}
