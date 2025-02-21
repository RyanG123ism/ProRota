using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Admin.Models.ViewModels
{
    public class CreateCompanyViewModel
    {
        public string ApplicationUserId { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(50, ErrorMessage = "Company name cannot exceed 50 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Company name can only contain letters, numbers, and spaces.")]
        public string CompanyName { get; set; }
        
    }
}
