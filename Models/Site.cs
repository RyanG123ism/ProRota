using System.ComponentModel.DataAnnotations.Schema;

namespace RotaPro.Models
{
    public class Site
    {

        public int Id { get; set; }

        public string SiteName { get; set; } = string.Empty;


        //navigational properties

        public ICollection<ApplicationUser>? ApplicationUsers { get; set; } = new List<ApplicationUser>();

        public ICollection<Shift>? Shifts { get; set; } = new List<Shift>();

        [ForeignKey("CompanyId")]
        public int? CompanyId { get; set; }

        public Company? Company { get; set; }




    }
}
