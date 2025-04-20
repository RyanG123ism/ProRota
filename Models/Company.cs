using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class Company
    {
        public int Id { get; set; }
        [Display(Name = "Company")]
        public string CompanyName { get; set; } = string.Empty;

        //navigational properties
        //stores the owner of the company
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }
        public ICollection<Site> Sites { get; set; } = new List<Site>();
    }
}
