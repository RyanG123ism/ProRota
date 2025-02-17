using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProRota.Areas.Management.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role Category is required.")]
        [MinLength(3, ErrorMessage = "Role Category must be at least 3 characters long.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Role Category can only contain letters.")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = "";

        [Display(Name = "Role Category")]
        public int SelectedRoleCategoryId { get; set; }

        public List<SelectListItem> RoleCategories { get; set; } = new List<SelectListItem>();
    }
}
