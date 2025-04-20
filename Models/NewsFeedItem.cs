using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProRota.Models
{
    public class NewsFeedItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Defines if it's for an Individual User, a Site, or a Company
        [Required]
        public NewsFeedTargetType TargetType { get; set; }

        //User who created the post (Post Owner)
        [Required]
        public string CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser CreatedByUser { get; set; }

        //If the post is for an Individual User
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        //If the post is for a Site
        public int? SiteId { get; set; }
        [ForeignKey("SiteId")]
        public virtual Site? Site { get; set; }

        //If the post is for a Company
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }

    public enum NewsFeedTargetType
    {
        User,    // Sent to a single user
        Site,    // Sent to all users in a site
        Company  // Sent to all users in a company
    }
}
