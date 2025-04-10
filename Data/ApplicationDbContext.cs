using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProRota.Models;

namespace ProRota.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Keep Identity configurations

            //User who CREATED the news item
            modelBuilder.Entity<NewsFeedItem>()
                .HasOne(n => n.CreatedByUser)
                .WithMany()
                .HasForeignKey(n => n.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            //User who RECEIVES the news item
            modelBuilder.Entity<NewsFeedItem>()
                .HasOne(n => n.ApplicationUser)
                .WithMany(u => u.NewsFeedItems) // Explicitly map this relationship
                .HasForeignKey(n => n.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<TimeOffRequest> TimeOffRequests { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SiteConfiguration> SiteConfigurations { get; set; }
        public DbSet<RoleCategory> RoleCategories { get; set; }
        public DbSet<RoleConfiguration> RoleConfigurations { get; set; }
        public DbSet<NewsFeedItem> NewsFeedItems { get; set; }


    }
}
