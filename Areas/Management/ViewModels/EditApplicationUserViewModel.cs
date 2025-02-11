using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Management.Models
{
    public class EditApplicationUserViewModel
    {

        //Email property with validation attributes
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        //First Name property with validation attributes
        [Required(ErrorMessage = "First Name is required")]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "First Name must be between 3-25 characters long")]
        [RegularExpression("[A-Za-z]+", ErrorMessage = "Invalid characters. Only alphabetic characters are allowed.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        //Last Name property with validation attributes
        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Last Name must be between 3-30 characters long")]
        [RegularExpression("[A-Za-z]+", ErrorMessage = "Invalid characters. Only alphabetic characters are allowed.")]
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
   

        //SelectedRole property to store the selected role
        [Display(Name = "Role")]
        public string? CurrentRole { get; set; }

        //List of available roles
        [Display(Name = "New Role")]
        [Required(ErrorMessage = "Please select a new role")]
        public string? Role { get; set; }

        //AvailableRoles property to store the list of available roles
        public ICollection<SelectListItem>? Roles { get; set; }

    }
}
