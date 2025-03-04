using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Models;

namespace ProRota.Services
{
    public class RotaService : IRotaService
    {
        private readonly ApplicationDbContext _context;

        public RotaService(ApplicationDbContext context)
        {
            _context = context;
        }
        public string CalculateNextSundayDateToString(Shift shift)
        {
            return CalculateNextSundayDateToString(shift.StartDateTime.Value);
        }

        public string CalculateNextSundayDateToString(DateTime date)
        {
            var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
            var endOfWeekDate = date.AddDays(daysUntilSunday);

            return endOfWeekDate.ToString("yyyy-MM-dd");
        }

        public async Task<Dictionary<string, Dictionary<string, List<Shift>>>> GeterateWeeklyRotaListForSiteAsync(int siteId)
        {
            //get shifts for the managers corresponding site for the last 12 weeks (84 days)
            var shifts = await _context.Shifts
                .Where(s => s.SiteId == siteId && s.StartDateTime > DateTime.Now.AddDays(-84))
                .ToListAsync();

            //dictionary for all weekly rotas
            var weeklyRotas = shifts
                .GroupBy(s => CalculateNextSundayDateToString(s))
                .OrderByDescending(g => DateTime.Parse(g.Key))
                .ToDictionary(g => g.Key, g => g.ToList());

            //dictionary to hold multiple dicitonaries of catagorised weekly rotas 
            var categorisedWeeklyRotas = new Dictionary<string, Dictionary<string, List<Shift>>>
            {
                { "Unpublished Rotas", new Dictionary<string, List<Shift>>() },
                { "Current Week", new Dictionary<string, List<Shift>>() },
                { "Published Rotas", new Dictionary<string, List<Shift>>() }
            };


            var activeWeekKey = CalculateNextSundayDateToString(DateTime.Now);

            foreach (var (weekEnding, shiftsInWeek) in weeklyRotas)
            {
                bool hasUnpublishedShifts = shiftsInWeek.Any(s => !s.IsPublished);

                if (weekEnding == activeWeekKey)
                    categorisedWeeklyRotas["Current Week"][weekEnding] = shiftsInWeek;
                else if (hasUnpublishedShifts)
                    categorisedWeeklyRotas["Unpublished Rotas"][weekEnding] = shiftsInWeek;
                else
                    categorisedWeeklyRotas["Published Rotas"][weekEnding] = shiftsInWeek;
            }

            //order categories by descending 
            categorisedWeeklyRotas["Unpublished Rotas"] = categorisedWeeklyRotas["Unpublished Rotas"]
                .OrderByDescending(kvp => DateTime.Parse(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            categorisedWeeklyRotas["Current Week"] = categorisedWeeklyRotas["Current Week"]
                .OrderByDescending(kvp => DateTime.Parse(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            categorisedWeeklyRotas["Published Rotas"] = categorisedWeeklyRotas["Published Rotas"]
                .OrderByDescending(kvp => DateTime.Parse(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return categorisedWeeklyRotas;
        }

        public async Task<Dictionary<string, ViewRotaViewModel>> GetWeeklyRota(int siteId, DateTime weekEndingDate)
        {
            var site = await _context.Sites
                .Where(s => s.Id == siteId)
                .Include(s => s.SiteConfiguration)
                .SingleOrDefaultAsync();

            if (site?.SiteConfiguration == null)
                throw new Exception("Site configuration not found.");

            var roleLookup = await GetRoleLookup(site.SiteConfiguration.Id);
            var users = await GetSiteUsers(siteId);
            var userRoles = await GetUserRoles(users.Select(u => u.Id).ToList());

            var weekStartingDate = weekEndingDate.AddDays(-6);// -6 days givesd us the monday
            var weekEndingDateExtended = weekEndingDate.AddHours(23); //adding hours so that all start times on sunday are picked up

            var rota = users.Select(u => new ViewRotaViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = userRoles.ContainsKey(u.Id) ? userRoles[u.Id] : "No Role",
                RoleCategory = userRoles.ContainsKey(u.Id) && roleLookup.ContainsKey(userRoles[u.Id])
                                ? roleLookup[userRoles[u.Id]]
                                : "No Category",
                Shifts = u.Shifts.Where(s => s.StartDateTime >= weekStartingDate && s.StartDateTime <= weekEndingDateExtended).ToList(),
                TimeOffRequests = u.TimeOffRequests.Where(t => t.Date >= weekStartingDate && t.Date <= weekEndingDateExtended && t.IsApproved == ApprovedStatus.Approved).ToList()
            })
            .OrderBy(u => u.RoleCategory)
            .ThenBy(u => u.Role)
            .ToDictionary(u => u.Id);

            return rota;
        }

        public async Task<Dictionary<string, ViewRotaViewModel>> CreateRotaViewModel(Dictionary<DateTime, Dictionary<string, List<Shift>>> rota, 
            Dictionary<string, List<TimeOffRequest>> timeOffRequests,
            int siteId, int siteConfigId)
        {

            var roleLookup = await GetRoleLookup(siteConfigId);
            var users = await GetSiteUsers(siteId);

            var userRoles = await GetUserRoles(users.Select(u => u.Id).ToList());

            //convert rota into a dictionary where the key is the User ID and value is a ViewRotaViewModel
            var viewModel = rota
                .SelectMany(r => r.Value) //flatten the dictionary
                .GroupBy(r => r.Key) //group shifts by UserId
                .ToDictionary(
                    g => g.Key,
                    g => new ViewRotaViewModel
                    {
                        Id = g.Key,
                        FirstName = g.First().Value.First().ApplicationUser?.FirstName ?? "Unknown",
                        LastName = g.First().Value.First().ApplicationUser?.LastName ?? "Unknown",
                        Role = userRoles.ContainsKey(g.Key) ? userRoles[g.Key] : "No Role",
                        RoleCategory = userRoles.ContainsKey(g.Key) && roleLookup.ContainsKey(userRoles[g.Key])
                                ? roleLookup[userRoles[g.Key]]
                                : "No Category",
                        Shifts = g.SelectMany(x => x.Value).ToList(), // Flatten all shifts into a single list
                        TimeOffRequests = timeOffRequests.ContainsKey(g.Key) ? timeOffRequests[g.Key] : new List<TimeOffRequest>()
                    });

            //user keys
            var assignedUserIds = new HashSet<string>(viewModel.Keys);

            //users that didnt get assigned any shifts
            var unasignedUsers = await _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed == true && !assignedUserIds.Contains(u.Id)).Include(u => u.TimeOffRequests.Where(t => t.IsApproved == ApprovedStatus.Approved)).ToListAsync() ?? null;

            if (unasignedUsers != null || unasignedUsers.Count > 0)
            {
                //adding unasigned users to the viewModel
                foreach (var user in unasignedUsers)
                {
                    viewModel.Add(user.Id, new ViewRotaViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = userRoles.ContainsKey(user.Id) ? userRoles[user.Id] : "No Role",
                        RoleCategory = userRoles.ContainsKey(user.Id) && roleLookup.ContainsKey(userRoles[user.Id])
                                ? roleLookup[userRoles[user.Id]]
                                : "No Category",
                        Shifts = new List<Shift>(),
                        TimeOffRequests = user.TimeOffRequests.ToList()
                    }); ;
                }
            }

            return viewModel;
        }

        public async Task<bool> SaveRota(Dictionary<string, ViewRotaViewModel> model, int siteId, bool publishStatus)
        {
            var userIds = model.Keys.ToList();
            var existingShifts = await _context.Shifts
                .Where(s => userIds.Contains(s.ApplicationUserId) && s.SiteId == siteId)
                .ToListAsync();

            var shiftsToAdd = new List<Shift>();
            var shiftsToUpdate = new List<Shift>();

            foreach (var userId in model.Keys)
            {
                foreach (var shift in model[userId].Shifts)
                {
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime == shift.StartDateTime &&
                        s.EndDateTime == shift.EndDateTime);

                    if (existingShift == null)
                    {
                        shift.ApplicationUserId = userId;
                        shift.SiteId = siteId;
                        shift.IsPublished = publishStatus;
                        shiftsToAdd.Add(shift);
                    }
                    else
                    {
                        existingShift.StartDateTime = shift.StartDateTime;
                        existingShift.EndDateTime = shift.EndDateTime;
                        existingShift.IsPublished = publishStatus;
                        shiftsToUpdate.Add(existingShift);
                    }
                }
            }

            if (shiftsToAdd.Any()) await _context.Shifts.AddRangeAsync(shiftsToAdd);
            if (shiftsToUpdate.Any()) _context.Shifts.UpdateRange(shiftsToUpdate);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EditRota(Dictionary<string, Dictionary<string, ShiftUpdateModel>> editedShifts, int siteId, bool publishStatus)
        {
            //list of userID's of users that have had their shifts edited
            List<string> affectedUserIds = editedShifts.Keys.ToList();

            //list of all shift dates from the editedShifts dictionary
            var shiftDates = editedShifts.Values
                .SelectMany(s => s.Keys)
                .Distinct()
                .Select(DateTime.Parse)
                .ToList();

            //queries the db for shifts that match the usersId and shift dates
            var existingShifts = await _context.Shifts
                .Where(s => affectedUserIds.Contains(s.ApplicationUserId) &&
                            s.StartDateTime.HasValue &&
                            shiftDates.Contains(s.StartDateTime.Value.Date) &&
                            s.SiteId == siteId)
                .ToListAsync();

            //placeholders for shifts to add, update, and delete
            List<Shift> shiftsToAdd = new List<Shift>();
            List<Shift> shiftsToUpdate = new List<Shift>();
            List<Shift> shiftsToDelete = new List<Shift>();

            //loops through each users edited shifts
            foreach (var userEntry in editedShifts)
            {
                string userId = userEntry.Key;//gets the user Id

                //loop through each shift 
                foreach (var shiftEntry in userEntry.Value)
                {
                    //get the new date and times for the shift
                    DateTime shiftDate = DateTime.Parse(shiftEntry.Key);
                    ShiftUpdateModel newShiftData = shiftEntry.Value;

                    //look for an existing shift
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime.HasValue &&
                        s.StartDateTime.Value.Date == shiftDate);

                    //if times are null - the shift was removed during editing
                    if (newShiftData.StartTime == null || newShiftData.EndTime == null)
                    {
                        //add to DELETE list
                        if (existingShift != null)
                        {
                            shiftsToDelete.Add(existingShift);
                        }
                    }
                    else
                    {
                        //get the start / end times
                        DateTime newStart = shiftDate.Date.Add(TimeSpan.Parse(newShiftData.StartTime));
                        DateTime newEnd = shiftDate.Date.Add(TimeSpan.Parse(newShiftData.EndTime));

                        //if no current shift exists then add to the ADD list 
                        if (existingShift == null)
                        {
                            shiftsToAdd.Add(new Shift
                            {
                                ApplicationUserId = userId,
                                SiteId = siteId,
                                StartDateTime = newStart,
                                EndDateTime = newEnd,
                                IsPublished = publishStatus // assigning publish status (from view)
                            });
                        }
                        else if (existingShift.StartDateTime.Value != newStart || existingShift.EndDateTime.Value != newEnd)
                        {
                            //if shift exists then add to UPDATE list
                            existingShift.StartDateTime = newStart;
                            existingShift.EndDateTime = newEnd;
                            shiftsToUpdate.Add(existingShift);
                        }
                    }
                }
            }
            //update the changes to the DB 
            if (shiftsToDelete.Any()) _context.Shifts.RemoveRange(shiftsToDelete);
            if (shiftsToAdd.Any()) await _context.Shifts.AddRangeAsync(shiftsToAdd);

            if (shiftsToUpdate.Any())
            {
                _context.Shifts.UpdateRange(shiftsToUpdate);
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UnpublishRota(Dictionary<string, ViewRotaViewModel> model, int siteId)
        {
            var thisSundayString = CalculateNextSundayDateToString(DateTime.Now);
            DateTime thisSunday = DateTime.Parse(thisSundayString);

            //check the provided model for shifts within or before the current week
            bool containsCurrentOrPastWeekShifts = model.Values
                .SelectMany(vm => vm.Shifts)
                .Any(shift => shift.StartDateTime <= thisSunday);

            if (containsCurrentOrPastWeekShifts)
            {
                return false; //return false if shifts are wiithin the current week
            }

            //get all existing shifts for the users in the rota
            var userIds = model.Keys.ToList();
            var existingShifts = await _context.Shifts
                .Where(s => userIds.Contains(s.ApplicationUserId) && s.SiteId == siteId)
                .ToListAsync();

            List<Shift> shiftsToUpdate = new List<Shift>();

            foreach (var userId in model.Keys)
            {
                foreach (var shift in model[userId].Shifts)
                {
                    // Find existing shift in the DB
                    var existingShift = existingShifts.FirstOrDefault(s =>
                        s.ApplicationUserId == userId &&
                        s.StartDateTime == shift.StartDateTime &&
                        s.EndDateTime == shift.EndDateTime &&
                        s.SiteId == siteId);

                    if (existingShift != null)
                    {
                        existingShift.IsPublished = false; // Unpublish shift
                        shiftsToUpdate.Add(existingShift);
                    }
                }
            }

            if (shiftsToUpdate.Any())
            {
                _context.Shifts.UpdateRange(shiftsToUpdate);
            }

            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<Dictionary<string, string>> GetRoleLookup(int siteConfigurationId)
        {
            var roleConfigs = await _context.RoleConfigurations
                .Where(rc => rc.SiteConfigurationId == siteConfigurationId)
                .Include(rc => rc.RoleCategory)
                    .ThenInclude(rc => rc.Roles)
                .ToListAsync();

            var roleLookup = new Dictionary<string, string>();

            foreach (var roleConfig in roleConfigs)
            {
                if (roleConfig.RoleCategory?.Roles != null)
                {
                    foreach (var role in roleConfig.RoleCategory.Roles)
                    {
                        roleLookup[role.Name] = roleConfig.RoleCategory.Name; // Role Name → Role Category
                    }
                }
            }

            return roleLookup;
        }

        public async Task<List<ApplicationUser>> GetSiteUsers(int siteId)
        {
            return await _context.ApplicationUsers
                .Where(u => u.SiteId == siteId && u.EmailConfirmed)
                .Include(u => u.Shifts)
                .Include(u => u.TimeOffRequests)
                .ToListAsync();
        }

        public async Task<Dictionary<string, string>> GetUserRoles(List<string> userIds)
        {
            //Fetch User Roles from Identity Tables
            var userRoles = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .Where(ur => userIds.Contains(ur.UserId)) //Only fetch roles for relevant users
                .ToDictionaryAsync(ur => ur.UserId, ur => ur.RoleName);

            return userRoles;
        }

    }
}
