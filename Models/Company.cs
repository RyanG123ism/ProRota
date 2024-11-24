using System.ComponentModel.DataAnnotations;

namespace RotaPro.Models
{
    public class Company
    {

        public int Id { get; set; }

        [Display(Name = "Company")]
        public string CompanyName { get; set; } = string.Empty;

        //navigational properties

        public ICollection<Site> Sites { get; set; } = new List<Site>();
    }
}
