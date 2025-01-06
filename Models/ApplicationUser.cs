using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Tracing;

namespace ProRota.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Salary")]
        public float Salary { get; set; } = 10.50f;

        [Display(Name = "Contractual Hours")]
        public int ContractualHours { get; set; } = 8;//minimum contract

        [Display(Name = "Holidays per Year")]
        public int HolidaysPerYear { get; set; } = 25;

        [Display(Name = "Remaining Holidays")]
        public int RemainingHolidays { get; set; } = 25;

        [Display(Name = "Employee Notes")]
        public string Notes { get; set; } = string.Empty; //for things like 'childcare on mondays' etc


        //Navigational properties

        [ForeignKey("SiteId")]
        public int? SiteId { get; set; }
        public Site? Site { get; set; }

        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

        public ICollection<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();

        
    }
}
