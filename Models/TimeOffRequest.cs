using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class TimeOffRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "Use a Holiday?")]
        public bool IsHoliday { get; set; }

        public ApprovedStatus IsApproved { get; set; } = ApprovedStatus.Pending;

        //navigational properties

        [ForeignKey("ApplicationUserId")]
        [JsonIgnore]
        public string ApplicationUserId { get; set; }
        [JsonIgnore]
        public ApplicationUser ApplicationUser { get; set; }

    }

    public enum ApprovedStatus
    {
        Approved,
        Denied,
        Pending
    }
}
