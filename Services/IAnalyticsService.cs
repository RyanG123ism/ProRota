using ProRota.Models;

namespace ProRota.Services
{
    public interface IAnalyticsService
    {
        Task<Dictionary<string, int>> GetTotalHoursBarChartValues(Site site, DateTime weekStartingDate, DateTime weekEndingDate);
        Task<Dictionary<string, int>> GetRoleDistributionPieChartValues(Site site, List<ApplicationUser> users, DateTime weekStart, DateTime weekEnd);
        Task<Dictionary<string, decimal>> GetWageDataLineGraphValues(Site site, Dictionary<string, int> totalHoursByDay, DateTime weekStart, DateTime weekEnd);
        Task<int[,]> GetShiftTimeHeatmapDataValues(Site site, DateTime weekStart, DateTime weekEnd);
    }
}
