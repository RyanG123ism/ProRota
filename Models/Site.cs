using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class Site
    {

        public int Id { get; set; }

        [Display(Name = "Name of Site:")]
        public string SiteName { get; set; } = string.Empty;

        public SiteConfiguration? SiteConfiguration { get; set; }

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

        //navigational properties

        public ICollection<ApplicationUser>? ApplicationUsers { get; set; } = new List<ApplicationUser>();

        public ICollection<Shift>? Shifts { get; set; } = new List<Shift>();

        [ForeignKey("CompanyId")]
        public int? CompanyId { get; set; }

        public Company? Company { get; set; }






    }
}
