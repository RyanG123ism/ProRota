using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Management.ViewModels
{
    public class EditRoleConfigurationViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Minimum Employees is required.")]
        [Range(1, 20, ErrorMessage = "Minimum Employees must be between 1 and 20.")]
        [Display(Name = "Minimum Employees Required")]
        public int MinEmployees { get; set; }

        [Required(ErrorMessage = "Maximum Employees is required.")]
        [Range(1, 20, ErrorMessage = "Maximum Employees must be between 1 and 20.")]
        [Display(Name = "Maximum Employees Required")]
        public int MaxEmployees { get; set; }

        [Required(ErrorMessage = "Role Category is required.")]
        [MinLength(3, ErrorMessage = "Role Category must be at least 3 characters long.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Role Category can only contain letters.")]
        [Display(Name = "Role Category")]
        public string SelectedRoleCategory { get; set; } = "";
    }
}
