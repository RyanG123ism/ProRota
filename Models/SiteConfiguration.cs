using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class SiteConfiguration
    {

        public int Id { get; set; }

        [Display(Name = "Standard Booking Duration")]
        [DisplayFormat(DataFormatString = @"{0:hh\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan BookingDuration { get; set; } = new TimeSpan(1, 45, 0);//default value of 1 hour, 45 mins

        [Display(Name = "Covers Capacity")]
        [Range(0, int.MaxValue, ErrorMessage = "Covers Capacity must be a non-negative number.")]
        public int? CoversCapacity { get; set; } = 0;

        [Display(Name = "Number of Sections")]
        [Range(0, int.MaxValue, ErrorMessage = "Number of Sections must be a non-negative number.")]
        public int? NumberOfSections { get; set; } = 0;

        [Display(Name = "Max Front of House")]
        [Range(0, int.MaxValue, ErrorMessage = "Max Front of House must be a non-negative number.")]
        public int? MaxFrontOfHouse { get; set; } = 0;

        [Display(Name = "Max Bartenders")]
        [Range(0, int.MaxValue, ErrorMessage = "Max Bartenders must be a non-negative number.")]
        public int? MaxBarTenders { get; set; } = 0;

        [Display(Name = "Max Management")]
        [Range(0, int.MaxValue, ErrorMessage = "Max Management must be a non-negative number.")]
        public int? MaxManagement { get; set; } = 0;

        [Display(Name = "Min Management")]
        [Required(ErrorMessage = "At least one manager or supervisor must be present.")]
        [Range(1, int.MaxValue, ErrorMessage = "Min Management must be at least 1.")]
        public int? MinManagement { get; set; } = 1;

        //navigational properties
        [ForeignKey("SiteId")]
        public int SiteId { get; set; }
        public Site Site { get; set; }

    }
}
