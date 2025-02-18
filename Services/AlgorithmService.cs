using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Models;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProRota.Services
{
    public class AlgorithmService : IAlgorithmService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;//accessing httpContext properties of controller base
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IServiceScopeFactory _scopeFactory; //to access usermanager

        public AlgorithmService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, RoleManager<ApplicationRole> roleManager, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _roleManager = roleManager;
            _scopeFactory = scopeFactory;
        }
        public async Task<int> CreateWeeklyRota(CreateWeeklyRotaViewModel viewModel, Site site)
        {
            if(viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if(site == null)
            {
                throw new ArgumentNullException(nameof(site));
            }

            //final rota variable that will be returned
            var rota = new Dictionary<DateTime, Dictionary<string, List<Tuple<string, TimeSpan, TimeSpan>>>>();

            var siteConfig = site.SiteConfiguration;
            var roleConfigs = site.SiteConfiguration.RoleConfigurations;

            var users = await _context.ApplicationUsers.Where(u => u.SiteId == site.Id)
                .Include(u => u.TimeOffRequests
                    .Where(t => t.Date >= viewModel.WeekEndingDate.AddDays(-6) && t.Date < viewModel.WeekEndingDate && t.IsApproved == ApprovedStatus.Approved))
                .ToListAsync();

            //STEP 1: Sort users by role
            var sortedUsers = await SortUsersByRole(users) ?? throw new Exception("Error Sorting Users by Role");

            //STEP 2: calculate daily covers and assess which days to prioritise
            var weeklyCoverBreakdown = viewModel.Covers;
            var dailyTotalCovers = CalculateDailyCoversByPriority(weeklyCoverBreakdown) ?? throw new Exception("Error calculating daily covers by priority"); ;

            //STEP3: calculate neccecary staff for each day

            //looping through days, starts with my significant (busiest) day
            foreach (var day in dailyTotalCovers)
            {
                //match the key value to the actual date
                var date = CalculateDateFromDayOfWeek(day.Key, viewModel.WeekEndingDate);

                //returns the staffing requirements for each day
                var requiredEmployees = CalculateDailyEmployeeRequirments(site, day, weeklyCoverBreakdown[day.Key]);

                var shiftTimes = CalculateShiftTimes(day.Key, date, requiredEmployees, site);

                var dayRota = AllocateShifts(shiftTimes, sortedUsers);
            }



            //STEP 4: Assign shifts to users

            //STEP 5: Review?

            //STEP 6: return dictionary

            return 0;
            
        }

        public async Task<Dictionary<string, List<ApplicationUser>>> SortUsersByRole(List<ApplicationUser> users)
        {
            if (users == null || users.Count == 0)
            {
                throw new Exception("Error accessing user list");
            }

            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var sortedUsersByRole = new Dictionary<string, List<ApplicationUser>>();

            var userRoles = new List<(ApplicationUser User, IList<string> Roles)>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userRoles.Add((user, roles));
            }

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

        public Dictionary<string, Dictionary<int, int>> CalculateDailyEmployeeRequirments(Site site, KeyValuePair<string, int> dailyCovers, Dictionary<int, int> coverBreakdwon)
        {
            //dictionary of all employee requirments that will be returned
            var employeeRequirments = new Dictionary<string, Dictionary<int, int>>();

            //covers for the day and maxCovers for site
            var covers = dailyCovers.Value;
            var maxCovers = site.SiteConfiguration.CoversCapacity;
            
            //role configurations for the site
            var roleConfigs = site.SiteConfiguration.RoleConfigurations;

            foreach(var config in roleConfigs)
            {
                var roleCategory = config.RoleCategory.Name;

                //gets the required number of employees for that role - for each quater of the day. 
                var requiredEmployees = CalculateDailyEmployeeRequirmentsForRole(config.MinEmployees, config.MaxEmployees, covers, maxCovers, coverBreakdwon);

                //adding the role categories requirments to the dictionary
                employeeRequirments.Add(roleCategory, requiredEmployees);
                
            }

            return employeeRequirments;
        }

        public Dictionary<int, int> CalculateDailyEmployeeRequirmentsForRole(int minEmployees, int maxEmployees, int covers, int? maxCovers, Dictionary<int, int> coverBreakdown)
        {
            // Validate maxCovers
            if (maxCovers == null || maxCovers == 0)
            {
                throw new ArgumentException("maxCovers must be a valid positive number.");
            }
                
            // Ensure total covers does not exceed maxCovers
            covers = Math.Min(covers, maxCovers.Value);

            // Calculate the staff scaling factor (0 = min employees, 1 = max employees)
            double staffScaleFactor = (double)covers / maxCovers.Value;

            // Determine total employees needed for the entire day
            int totalEmployees = (int)Math.Round(minEmployees + (maxEmployees - minEmployees) * staffScaleFactor);

            // Ensure total employees are within the min-max range
            totalEmployees = Math.Clamp(totalEmployees, minEmployees, maxEmployees);

            // Calculate the total covers for the day based on breakdown
            int totalBreakdownCovers = coverBreakdown.Values.Sum();

            // Dictionary to store employee allocation per quarter
            Dictionary<int, int> employeeAllocation = new Dictionary<int, int>();

            // Allocate employees based on quarter activity
            foreach (var kvp in coverBreakdown)
            {
                int quarter = kvp.Key;
                int quarterCovers = kvp.Value;

                // Avoid division by zero in case of no covers
                double quarterCoverPercentage = totalBreakdownCovers == 0 ? 0 : (double)quarterCovers / totalBreakdownCovers;

                // Distribute employees proportionally based on quarter's cover percentage
                int quarterEmployees = (int)Math.Round(totalEmployees * quarterCoverPercentage);

                // Ensure at least one employee is present during working hours
                quarterEmployees = Math.Max(quarterEmployees, minEmployees > 0 ? 1 : 0);

                employeeAllocation[quarter] = quarterEmployees;
            }

            return employeeAllocation;
        }

        public Dictionary<DateTime, Dictionary<string, List<Tuple<TimeSpan, TimeSpan>>>> CalculateShiftTimes(string day, DateTime date,Dictionary<string, Dictionary<int, int>> employeeAllocation, Site site)
        {
            // Dictionary to store calculated shift times
            var shiftSchedule = new Dictionary<DateTime, Dictionary<string, List<Tuple<TimeSpan, TimeSpan>>>>();

            // Site opening and closing times
            var (openingTime, closingTime) = GetOpeningAndClosingTimes(site, day);
            
            // Calculate duration of each quarter
            int numQuarters = 4;
            TimeSpan totalOperatingHours = closingTime - openingTime;
            TimeSpan quarterDuration = TimeSpan.FromMinutes(totalOperatingHours.TotalMinutes / numQuarters);

            // Initialize the date entry
            shiftSchedule[date] = new Dictionary<string, List<Tuple<TimeSpan, TimeSpan>>>();

            // Iterate over each role category
            foreach (var roleCategory in employeeAllocation)
            {
                string category = roleCategory.Key;
                var quarterDemands = roleCategory.Value;

                // Sort quarter demands to track when employees are needed
                var sortedQuarters = quarterDemands.OrderBy(q => q.Key).ToList();

                // Track assigned employees and their shifts
                List<Tuple<TimeSpan, TimeSpan>> shifts = new List<Tuple<TimeSpan, TimeSpan>>();
                List<Tuple<int, TimeSpan>> activeEmployees = new List<Tuple<int, TimeSpan>>(); // Stores <EmployeeID, StartTime>
                int employeeCounter = 1; // Unique employee ID tracker

                // Iterate through each quarter and assign shifts
                for (int i = 0; i < sortedQuarters.Count; i++)
                {
                    int quarterIndex = sortedQuarters[i].Key;
                    int employeesRequired = sortedQuarters[i].Value;
                    TimeSpan quarterStartTime = openingTime + (quarterDuration * (quarterIndex - 1));
                    TimeSpan quarterEndTime = quarterStartTime + quarterDuration;

                    // If more employees are needed, add new employees
                    while (activeEmployees.Count < employeesRequired)
                    {
                        activeEmployees.Add(new Tuple<int, TimeSpan>(employeeCounter, quarterStartTime)); // Add employee
                        employeeCounter++;
                    }

                    // If fewer employees are needed in the next quarter, remove the earliest-starting ones first
                    if (i < sortedQuarters.Count - 1)
                    {
                        int nextQuarterDemand = sortedQuarters[i + 1].Value;
                        if (nextQuarterDemand < activeEmployees.Count)
                        {
                            int employeesToRemove = activeEmployees.Count - nextQuarterDemand;

                            // Sort by start time and remove the earliest-starting employees first
                            activeEmployees = activeEmployees
                                .OrderBy(emp => emp.Item2)
                                .ToList();

                            for (int j = 0; j < employeesToRemove; j++)
                            {
                                var leavingEmployee = activeEmployees.First();
                                shifts.Add(new Tuple<TimeSpan, TimeSpan>(leavingEmployee.Item2, quarterEndTime));
                                activeEmployees.Remove(leavingEmployee);
                            }
                        }
                    }
                }

                // Ensure remaining employees work only as long as needed
                foreach (var employee in activeEmployees)
                {
                    shifts.Add(new Tuple<TimeSpan, TimeSpan>(employee.Item2, closingTime));
                }

                // Add shifts to the dictionary
                shiftSchedule[date][category] = shifts;
            }

            return shiftSchedule;
        }

        public Dictionary<string, List<Tuple<string, TimeSpan, TimeSpan>>>AllocateShifts(Dictionary<string, List<Tuple<TimeSpan, TimeSpan>>> shiftTimes,
                   Dictionary<string, List<ApplicationUser>> users)
        {
            // Dictionary to store assigned shifts
            var assignedShifts = new Dictionary<string, List<Tuple<string, TimeSpan, TimeSpan>>>();

            // Dictionary to track hours already assigned during this runtime
            var assignedHours = users
                .SelectMany(kvp => kvp.Value.Select(user => new { user.Id, Hours = 0.0 }))
                .ToDictionary(x => x.Id, x => x.Hours);

            // Iterate through each role category in shiftTimes
            foreach (var roleCategory in shiftTimes)
            {
                string category = roleCategory.Key;
                var shifts = roleCategory.Value;

                // Get the users assigned to this role category
                if (!users.ContainsKey(category) || users[category].Count == 0)
                {
                    Console.WriteLine($"No available users for {category}");
                    continue;
                }

                // Sort users into those **below** and **above** contractual hours
                var usersBelowContract = users[category]
                    .Where(user => assignedHours[user.Id] < user.ContractualHours)
                    .OrderBy(user => assignedHours[user.Id]) // Prioritize those with fewer assigned hours
                    .ToList();

                var usersAboveContract = users[category]
                    .Where(user => assignedHours[user.Id] >= user.ContractualHours)
                    .OrderBy(user => assignedHours[user.Id]) // Prioritize fairness after contract hours
                    .ToList();

                // Merge lists, prioritizing those below contract first
                var sortedUsers = usersBelowContract.Concat(usersAboveContract).ToList();

                if (sortedUsers.Count == 0)
                {
                    Console.WriteLine($"No available users for {category}");
                    continue;
                }

                // Assign shifts
                foreach (var shift in shifts)
                {
                    TimeSpan shiftStart = shift.Item1;
                    TimeSpan shiftEnd = shift.Item2;
                    TimeSpan shiftDuration = shiftEnd - shiftStart;
                    double shiftHours = shiftDuration.TotalHours;

                    // Find the best user to assign
                    var userToAssign = sortedUsers
                        .Where(user => !user.TimeOffRequests.Any(to => to.Date == shiftTimes)) // Ensure no time off conflicts
                        .FirstOrDefault();

                    if (userToAssign != null)
                    {
                        // Assign shift
                        if (!assignedShifts.ContainsKey(category))
                            assignedShifts[category] = new List<Tuple<string, TimeSpan, TimeSpan>>();

                        assignedShifts[category].Add(
                            new Tuple<string, TimeSpan, TimeSpan>(userToAssign.Id, shiftStart, shiftEnd) // Store User ID instead of Name
                        );

                        // Update dynamically tracked assigned hours
                        assignedHours[userToAssign.Id] += shiftHours;

                        // Resort users dynamically to maintain fairness
                        sortedUsers = sortedUsers.OrderBy(user => assignedHours[user.Id]).ToList();
                    }
                }
            }

            return assignedShifts;
        }



        public (TimeSpan openingTime, TimeSpan closingTime) GetOpeningAndClosingTimes(Site site, string day)
        {
            string openingPropertyName = $"{day}OpeningTime";
            string closingPropertyName = $"{day}ClosingTime"; 

            var openingTime = (TimeSpan?)site.GetType().GetProperty(openingPropertyName)?.GetValue(site) ?? TimeSpan.Zero;
            var closingTime = (TimeSpan?)site.GetType().GetProperty(closingPropertyName)?.GetValue(site) ?? TimeSpan.Zero;

            return (openingTime, closingTime);
        }





    }
}
