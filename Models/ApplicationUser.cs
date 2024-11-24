using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Tracing;

namespace RotaPro.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Salary")]
        public float Salary { get; set; }

        [Display(Name = "Contractual Hours")]
        public int ContractualHours { get; set; } = 0;

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
