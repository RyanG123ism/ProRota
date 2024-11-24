using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RotaPro.Models
{
    public class TimeOffRequest
    {
        
        public int Id { get; set; }

        [Required]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "Use a Holiday?")]
        public bool IsHoliday { get; set; }

        //navigational properties

        [ForeignKey("ApplicationUserId")]
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }




    }
}
