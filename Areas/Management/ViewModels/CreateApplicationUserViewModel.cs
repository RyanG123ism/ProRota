using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Management.Models
{
    public class CreateApplicationUserViewModel
    {
        //Email property with validation attributes
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        //Password property with validation attributes
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
                           ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        //First Name property with validation attributes
        [Required(ErrorMessage = "First Name is required")]
        [DataType(DataType.Text)]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "First Name must be between 3-25 characters long")]
        [RegularExpression("[A-Za-z]+", ErrorMessage = "Your first name contains invalid characters. Only alphabetic characters are allowed.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        //Last Name property with validation attributes
        [Required(ErrorMessage = "Last Name is required")]
        [DataType(DataType.Text)]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Last Name must be between 3-30 characters long")]
        [RegularExpression("[A-Za-z]+", ErrorMessage = "Your last name contains invalid characters. Only alphabetic characters are allowed.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        //Salary property with validation attributes
        [Required(ErrorMessage = "Salary is required")]
        [DataType(DataType.Currency, ErrorMessage = "Please enter a value that represents a monetary amount")]
        [Range(10.50, 1000, ErrorMessage = "Salary must be between {1} and {2}.")]
        [Display(Name = "Salary (Hourly)")]
        public float Salary { get; set; }

        //Contractual Hours property with validation attributes
        [Required(ErrorMessage = "COntractual Hours is required")]
        [Display(Name = "Contractual Hours")]
        [Range(8, 60, ErrorMessage = "Contractual Hours must be betwene 8-60")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Contractual Hours must be a valid number.")]
        public int ContractualHours { get; set; }

        //Notes property with validation attributes
        [Display(Name = "Employee Notes")]
        [DataType(DataType.Text)]
        public string Notes { get; set; } = string.Empty; //for things like 'childcare on mondays' etc

        //AvailableRoles property to store the list of available roles
        public ICollection<SelectListItem>? Roles { get; set; }

        //SelectedRole property to store the selected role
        [Required(ErrorMessage = "Role Selection is required")]
        [Display(Name = "Role")]
        public string Role { get; set; }
    }
}
