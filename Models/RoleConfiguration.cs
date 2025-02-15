using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProRota.Models
{
    public class RoleConfiguration
    {
        public int Id { get; set; }
        public int MinEmployees { get; set; }
        public int MaxEmployees { get; set; }

        [ForeignKey("RoleCategoryId")]
        public int RoleCategoryId { get; set; }
        public virtual RoleCategory RoleCategory { get; set; }

    }
}
