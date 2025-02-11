using ProRota.Models;

namespace ProRota.Services
{
    public class RotaService : IRotaService
    {
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

    }
}
