using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class ApplicationRole : IdentityRole
    {
        public int? RoleCategoryId { get; set; } //foreign Key to RoleCategory

        [ForeignKey("RoleCategoryId")]
        public virtual RoleCategory RoleCategory { get; set; }
    }
}
