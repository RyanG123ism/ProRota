namespace ProRota.Areas.Management.ViewModels
{
    public class AnalyticsDashboardViewModel
    {
        public Dictionary<string, int> TotalHoursByDay { get; set; }
        public Dictionary<string, int> StaffingByRole { get; set; }
        public Dictionary<string, decimal> WagesExpenditureByDay { get; set; }
        public int[,] HeatmapData { get; set; } = new int[24, 7];
    }

}
