using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Management.ViewModels
{
    public class CreateWeeklyRotaViewModel
    {
        [Required]
        public DateTime WeekEndingDate { get; set; }

        public Dictionary<string, Dictionary<int, int>> Covers { get; set; } = new Dictionary<string, Dictionary<int, int>>();
    }
}
