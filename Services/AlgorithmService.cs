using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Models;
using System.Runtime.InteropServices;

namespace ProRota.Services
{
    public class AlgorithmService : IAlgorithmService
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;//accessing httpContext properties of controller base
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AlgorithmService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _roleManager = roleManager;
        }
        public async void CreateWeeklyRota(CreateWeeklyRotaViewModel viewModel, Site site)
        {
            if(viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if(site == null)
            {
                throw new ArgumentNullException(nameof(site));
            }

            //FINAL ROTA VARIABLE THAT WILL BE RETURNED
            //var rota

            //STEP 1: Get and sort users by role

            var users = await _context.ApplicationUsers.Where(u => u.SiteId == site.Id).ToListAsync();

            //returns dictionary of users sorted by their roles as the key
            var sortedUsers = await SortUsersByRole(users);

            //STEP 2: calculate daily covers and assess which days to prioritise
            var weeklyCoverBreakdown = viewModel.Covers;
            var dailyTotalCovers = CalculateDailyCoversByPriority(weeklyCoverBreakdown);

            //STEP3: calculate neccecary staff for each day

            //looping through days, starts with my significant (busiest) day
            foreach (var day in dailyTotalCovers)
            {
                //match the key value to the actual date
                var date = CalculateDateFromDayOfWeek(day.Key, viewModel.WeekEndingDate);

                //returns the staffing requirements for each day
                var requiredEmployees = CalculateDailyEmployeeRequirments(site, day, weeklyCoverBreakdown[day.Key]);
            }

            
            
            //STEP 4: Assign shifts to users

            //STEP 5: Review?

            //STEP 6: return dictionary

            
        }

        public async Task<Dictionary<string, List<ApplicationUser>>> SortUsersByRole(List<ApplicationUser> users)
        {
            if (users == null || users.Count == 0)
            {
                throw new Exception("Error accessing user list");
            }

            var sortedUsersByRole = new Dictionary<string, List<ApplicationUser>>();

            //iterates through all users and fetches their role
            //'whenAll' is used to make sure all async tasks complete before moving on
            var userRoles = await Task.WhenAll(users.Select(async u =>
                new { User = u, Roles = await _userManager.GetRolesAsync(u) }
            ));

            foreach (var userRole in userRoles)
            {
                var roleName = userRole.Roles.FirstOrDefault() ?? "No Role";

                if (!sortedUsersByRole.ContainsKey(roleName))
                {
                    sortedUsersByRole[roleName] = new List<ApplicationUser>();
                }

                sortedUsersByRole[roleName].Add(userRole.User);
            }

            return sortedUsersByRole;
        }

        public Dictionary<string, int> CalculateDailyCoversByPriority(Dictionary<string, Dictionary<int, int>> covers)
        {
            if (covers is null || covers.Count == 0)
            {
                throw new ArgumentException("Error retrieving covers from view model");
            }

            //returns the covers with the day as the key, and a single value of all quaters summed
            var dailyCovers = covers.ToDictionary(
                day => day.Key,
                day => day.Value.Values.Sum()
            );

            //ordering by the highest daily cover value
            var prioritisedDailyCovers = dailyCovers.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            return prioritisedDailyCovers;
        }

        public DateTime CalculateDateFromDayOfWeek(string day, DateTime date)
        {
            if(!Enum.TryParse(day, true, out DayOfWeek dayOfWeek))
            {
                throw new Exception("Invalid day input parameter");              
            }

            var dayEnum = Convert.ToInt32(dayOfWeek);

            var remainder = ((int)DayOfWeek.Sunday - dayEnum + 7) % 7;
            var dayOfWeekDate = date.AddDays(-remainder);

            return dayOfWeekDate;
        }

        public Dictionary<string, Dictionary<string, int>> CalculateDailyEmployeeRequirments(Site site, KeyValuePair<string, int> dailyCovers, Dictionary<int, int> coverBreakdwon)
        {

            return new Dictionary<string, Dictionary<string, int>>();
        }
    }
}
