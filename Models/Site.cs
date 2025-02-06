using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class Site
    {

        public int Id { get; set; }

        [Display(Name = "Name of Site:")]
        public string SiteName { get; set; } = string.Empty;

        [Display(Name = "Monday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? MondayOpenTime { get; set; } = null;

        [Display(Name = "Monday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? MondayCloseTime { get; set; } = null;

        [Display(Name = "Tuesday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? TuesdayOpenTime { get; set; } = null;

        [Display(Name = "Tuesday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? TuesdayCloseTime { get; set; } = null;

        [Display(Name = "Wednesday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? WednesdayOpenTime { get; set; } = null;

        [Display(Name = "Wednesday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? WednesdayCloseTime { get; set; } = null;

        [Display(Name = "Thursday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ThursdayOpenTime { get; set; } = null;

        [Display(Name = "Thursday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ThursdayCloseTime { get; set; } = null;

        [Display(Name = "Friday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FridayOpenTime { get; set; } = null;

        [Display(Name = "Friday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FridayCloseTime { get; set; } = null;

        [Display(Name = "Saturday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? SaturdayOpenTime { get; set; } = null;

        [Display(Name = "Saturday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? SaturdayCloseTime { get; set; } = null;

        [Display(Name = "Sunday Open Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? SundayOpenTime { get; set; } = null;

        [Display(Name = "Sunday Close Time")]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? SundayCloseTime { get; set; } = null;

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

        [Display(Name = "Configuration Complete")]
        public bool ConfigurationComplete { get; set; } = false;

        //navigational properties

        public ICollection<ApplicationUser>? ApplicationUsers { get; set; } = new List<ApplicationUser>();

        public ICollection<Shift>? Shifts { get; set; } = new List<Shift>();

        [ForeignKey("CompanyId")]
        public int? CompanyId { get; set; }

        public Company? Company { get; set; }






    }
}
