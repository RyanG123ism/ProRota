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

        //navigational properties
        [ForeignKey("SiteId")]
        public int SiteId { get; set; }
        public Site Site { get; set; }
        public ICollection<RoleConfiguration>? RoleConfigurations { get; set; } = new List<RoleConfiguration>();

        

    }
}
