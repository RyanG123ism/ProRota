namespace ProRota.Models
{
    public class RoleCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "FOH", "BOH", "Admin"

        // Navigation property
        public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
    }

}
