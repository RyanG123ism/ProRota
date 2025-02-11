using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProRota.Models;

namespace ProRota.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<Shift> Shifts { get; set; }

        public DbSet<TimeOffRequest> TimeOffRequests { get; set; }

        public DbSet<Site> Sites { get; set; }

        public DbSet<SiteConfiguration> SiteConfigurations { get; set; }

    }
}
