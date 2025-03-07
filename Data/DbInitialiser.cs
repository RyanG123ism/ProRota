using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using ProRota.Models;
using ProRota.Services;
using System.Security.Claims;
using static Azure.Core.HttpHeader;

namespace ProRota.Data
{
    public class DbInitialiser
    {

        private readonly IServiceProvider _services;

        public DbInitialiser(IServiceProvider services)
        {
            _services = services;
        }

        public async Task Seed()
        {
            try
            {
                await SeedSite(_services);
                await SeedRoles(_services);
                await SeedSiteRoles(_services);
                await SeedCompany(_services);
                await SeedUsers(_services);
                await SeedClaims(_services);
                await SeedShifts(_services);
                await SeedTimeOffRequests(_services);

            }
            catch (Exception ex)
            {
                var logger = _services.GetRequiredService<ILogger<DbInitialiser>>();
                logger.LogError(ex, "An error occurred during database seeding.");
            }

        }
        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            //partial user - this gets assigned to users when first creating an account - before the fully register
            var partialUserRole_Unpaid = new ApplicationRole
            {
                Name = "Partial_User_Unpaid"
            };

            //partial user - assigned to user after payment completed - still to full set up company account
            var partialUserRole_Paid = new ApplicationRole
            {
                Name = "Partial_User_Paid"
            };

            var owner = new ApplicationRole
            {
                Name = "Owner"
            };

            await roleManager.CreateAsync(partialUserRole_Unpaid);
            await roleManager.CreateAsync(partialUserRole_Paid);
            await roleManager.CreateAsync(owner);
            await context.SaveChangesAsync();
        }
        public static async Task SeedSiteRoles(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Define Role Categories
            var roleCategories = new Dictionary<string, List<string>>
            {
                { "FOH", new List<string> { "Front of House Assistant", "Head Waiter" } },
                { "Bar", new List<string> { "Bartender", "Head Bartender" } },
                { "Management", new List<string> { "Supervisor", "Assistant Manager", "General Manager" } },
                { "BOH", new List<string> { "Chef de Partie", "Sous Chef", "Head Chef" } },
                { "Admin", new List<string> { "Admin" } },
                { "Deactivated", new List<string> { "Deactivated" } },
            };

            foreach (var category in roleCategories)
            {
                // Check if RoleCategory exists, if not, create it
                var roleCategory = await context.RoleCategories.FirstOrDefaultAsync(c => c.Name == category.Key);
                if (roleCategory == null)
                {
                    roleCategory = new RoleCategory { Name = category.Key };
                    context.RoleCategories.Add(roleCategory);
                    await context.SaveChangesAsync();
                }

                // Add roles under this category
                foreach (var role in category.Value)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var identityRole = new ApplicationRole
                        {
                            Name = role,
                            RoleCategoryId = roleCategory.Id
                        };
                        await roleManager.CreateAsync(identityRole);
                    }
                }

                // Associate this RoleCategory with RoleConfigurations for each Site
                var sites = await context.Sites.Include(s => s.SiteConfiguration).ToListAsync();

                foreach (var site in sites)
                {
                    if (site.SiteConfiguration == null)
                    {
                        site.SiteConfiguration = new SiteConfiguration();
                        context.SiteConfigurations.Add(site.SiteConfiguration);
                        await context.SaveChangesAsync();
                    }

                    // Check if RoleConfiguration already exists
                    var existingRoleConfig = await context.RoleConfigurations
                        .FirstOrDefaultAsync(rc => rc.RoleCategoryId == roleCategory.Id && rc.SiteConfigurationId == site.SiteConfiguration.Id);

                    if (existingRoleConfig == null)
                    {
                        var roleConfig = new RoleConfiguration
                        {
                            RoleCategoryId = roleCategory.Id,
                            SiteConfigurationId = site.SiteConfiguration.Id,
                            MinEmployees = 1,
                            MaxEmployees = 5
                        };

                        context.RoleConfigurations.Add(roleConfig);
                    }
                }

                await context.SaveChangesAsync();
            }
        }


        private async Task SeedUsers(IServiceProvider services)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            var users = new List<ApplicationUser>();
            var sites = dbContext.Sites.ToList();

            try
            {

                //adding admin user and asigning role if the account doesnt exist already 
                var adminUser = await userManager.FindByEmailAsync("admin@admin.com");

                if (adminUser == null)
                {
                    //creating admin user
                    var admin = new ApplicationUser()
                    {
                        FirstName = "admin",
                        LastName = "admin",
                        UserName = "admin@admin.com",
                        Email = "admin@admin.com",
                        NormalizedUserName = "ADMIN@ADMIN.COM",
                        NormalizedEmail = "ADMIN@ADMIN.COM",
                        PasswordHash = hasher.HashPassword(null!, "admin"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                       
                    };

                    await userManager.CreateAsync(admin, "admin");
                    await userManager.AddToRoleAsync(admin, "Admin");
                    users.Add(admin);
                }   

                //adding general manager user and asigning role if the account doesnt exist already 
                var generalManagerUser = await userManager.FindByEmailAsync("generalmanager@sixbynico.co.uk");

                if (generalManagerUser == null)
                {
                    var generalManager = new ApplicationUser()
                    {
                        FirstName = "Chris",
                        LastName = "Taylor",
                        UserName = "generalmanager@sixbynico.co.uk",
                        Email = "generalmanager@sixbynico.co.uk",
                        NormalizedUserName = "GENERALMANAGER@SIXBYNICO.CO.UK",
                        NormalizedEmail = "GENERALMANAGER@SIXBYNICO.CO.UK",
                        PasswordHash = hasher.HashPassword(null!, "sbn"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    };

                    await userManager.CreateAsync(generalManager, "sbn");
                    await userManager.AddToRoleAsync(generalManager, "General Manager");
                    users.Add(generalManager);
                }

                //adding assistant manager user and asigning role if the account doesnt exist already
                var assistantManagerUser = await userManager.FindByEmailAsync("assistantmanager@sixbynico.co.uk");

                if (assistantManagerUser == null)
                {
                    var assistantManager = new ApplicationUser()
                    {
                        FirstName = "Amy",
                        LastName = "Duncan",
                        UserName = "assistantmanager@sixbynico.co.uk",
                        Email = "assistantmanager@sixbynico.co.uk",
                        NormalizedUserName = "ASSISTANTMANAGER@SIXBYNICO.CO.UK",
                        NormalizedEmail = "ASSISTANTMANAGER@SIXBYNICO.CO.UK",
                        PasswordHash = hasher.HashPassword(null!, "sbn"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    };

                    await userManager.CreateAsync(assistantManager, "sbn");
                    await userManager.AddToRoleAsync(assistantManager, "Assistant Manager");
                    users.Add(assistantManager);
                }

                //adding supervisor role and user if supervisor doesnt exist already
                var supervisorUser = await userManager.FindByEmailAsync("supervisor@sixbynico.co.uk");

                if (supervisorUser == null)
                {
                    var supervisor = new ApplicationUser()
                    {
                        FirstName = "Ryan",
                        LastName = "Grant",
                        UserName = "supervisor@sixbynico.co.uk",
                        Email = "supervisor@sixbynico.co.uk",
                        NormalizedUserName = "SUPERVISOR@SIXBYNICO.CO.UK",
                        NormalizedEmail = "SUPERVISOR@SIXBYNICO.CO.UK",
                        PasswordHash = hasher.HashPassword(null!, "sbn"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    };

                    await userManager.CreateAsync(supervisor, "sbn");
                    await userManager.AddToRoleAsync(supervisor, "Supervisor");
                    users.Add(supervisor);
                }

                var frontOfHouseUsers = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        FirstName = "Bob",
                        LastName = "Dylan",
                        UserName = "bob@gmail.com",
                        Email = "bob@gmail.com",
                        NormalizedUserName = "BOB@GMAIL.COM",
                        NormalizedEmail = "BOB@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Alice",
                        LastName = "Johnson",
                        UserName = "alice@gmail.com",
                        Email = "alice@gmail.com",
                        NormalizedUserName = "ALICE@GMAIL.COM",
                        NormalizedEmail = "ALICE@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Charlie",
                        LastName = "Brown",
                        UserName = "charlie@gmail.com",
                        Email = "charlie@gmail.com",
                        NormalizedUserName = "CHARLIE@GMAIL.COM",
                        NormalizedEmail = "CHARLIE@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Diana",
                        LastName = "Prince",
                        UserName = "diana@gmail.com",
                        Email = "diana@gmail.com",
                        NormalizedUserName = "DIANA@GMAIL.COM",
                        NormalizedEmail = "DIANA@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Ethan",
                        LastName = "Hunt",
                        UserName = "ethan@gmail.com",
                        Email = "ethan@gmail.com",
                        NormalizedUserName = "ETHAN@GMAIL.COM",
                        NormalizedEmail = "ETHAN@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Fiona",
                        LastName = "Smith",
                        UserName = "fiona@gmail.com",
                        Email = "fiona@gmail.com",
                        NormalizedUserName = "FIONA@GMAIL.COM",
                        NormalizedEmail = "FIONA@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "George",
                        LastName = "Harrison",
                        UserName = "george@gmail.com",
                        Email = "george@gmail.com",
                        NormalizedUserName = "GEORGE@GMAIL.COM",
                        NormalizedEmail = "GEORGE@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Hannah",
                        LastName = "Davis",
                        UserName = "hannah@gmail.com",
                        Email = "hannah@gmail.com",
                        NormalizedUserName = "HANNAH@GMAIL.COM",
                        NormalizedEmail = "HANNAH@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Ian",
                        LastName = "McGregor",
                        UserName = "ian@gmail.com",
                        Email = "ian@gmail.com",
                        NormalizedUserName = "IAN@GMAIL.COM",
                        NormalizedEmail = "IAN@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Julia",
                        LastName = "Roberts",
                        UserName = "julia@gmail.com",
                        Email = "julia@gmail.com",
                        NormalizedUserName = "JULIA@GMAIL.COM",
                        NormalizedEmail = "JULIA@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                };

                //adds each user and asigns them to foh assistant Role
                foreach (var user in frontOfHouseUsers)
                {
                    // Create the user
                    var result = await userManager.CreateAsync(user, "password");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Front of House Assistant");
                        users.Add(user);
                    }
                }

                var bartenders = new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        FirstName = "Ian",
                        LastName = "McGregor",
                        UserName = "ian@gmail.com",
                        Email = "ian@gmail.com",
                        NormalizedUserName = "IAN@GMAIL.COM",
                        NormalizedEmail = "IAN@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    },

                    new ApplicationUser
                    {
                        FirstName = "Julia",
                        LastName = "Roberts",
                        UserName = "julia@gmail.com",
                        Email = "julia@gmail.com",
                        NormalizedUserName = "JULIA@GMAIL.COM",
                        NormalizedEmail = "JULIA@GMAIL.COM",
                        PasswordHash = hasher.HashPassword(null!, "password"),
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        PhoneNumberConfirmed = true,
                        Salary = 15,
                        ContractualHours = 45,
                        Notes = "none"
                    }
                };

                //adds each user and asigns them to 'Member' Role
                foreach (var user in bartenders)
                {
                    // Create the user
                    var result = await userManager.CreateAsync(user, "password");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Bartender");
                        users.Add(user);
                    }
                }

                var faker = new Faker();

                foreach (var user in users)
                {                 
                    if(user.Email != "admin@admin.com")
                    {
                        user.Site = faker.PickRandom(sites.Where((site, index) => index != 2)); //any site bu index 2 - No Site - for admin and other special users
                    }
                    else
                    {
                        user.Site = sites[2]; //gives admin a "No Site" site. 
                    }
                }

                await dbContext.SaveChangesAsync();


            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding roles and users.");
            }
        }

        public async Task SeedClaims(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var claimsService = services.GetRequiredService<IClaimsService>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Fetch existing seeded users and sites
            var users = await dbContext.ApplicationUsers.ToListAsync();
            var sites = await dbContext.Sites.ToListAsync();

            //assign a site to each user if they don’t have one
            var faker = new Faker();
            foreach (var user in users)
            {
                // Assign a SiteId if not already assigned
                if (user.SiteId == 0)
                {
                    user.SiteId = faker.PickRandom(sites.Where(s => s.SiteName != "No Site")).Id;
                }

                // ✅ Ensure claims are added to the user
                var existingClaims = await userManager.GetClaimsAsync(user);

                // Add SiteId claim if it doesn't exist
                if (!existingClaims.Any(c => c.Type == "SiteId"))
                {
                    await userManager.AddClaimAsync(user, new Claim("SiteId", user.SiteId.ToString()));
                }

                // Add CompanyId claim if it doesn't exist
                if (!existingClaims.Any(c => c.Type == "CompanyId"))
                {
                    await userManager.AddClaimAsync(user, new Claim("CompanyId", "1")); // Defaulting CompanyId to 1
                }
            }

            // Save changes to DB
            await dbContext.SaveChangesAsync();
        }

        public async Task SeedShifts(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var users = dbContext.ApplicationUsers.ToList();

            var sites = dbContext.Sites.ToList();

            try
            {
                if (!dbContext.Shifts.Any())
                {
                    var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek); // Start of the week
                    var endOfWeek = startOfWeek.AddDays(7).AddSeconds(-1); // End of the week

                    var faker = new Faker<Shift>()
                        //Start Date between NOW and -70 days ago and a TIME betwen 10:00 and 13:00
                        .RuleFor(s => s.StartDateTime, f => f.Date.Between(DateTime.Now.AddDays(-70), DateTime.Now).Date.Add(
                            f.Date.Between(DateTime.Now.Date, DateTime.Now.Date).TimeOfDay.Add(TimeSpan.FromHours(f.Random.Int(10, 13)))))
                        //End Date between startDate time and 10+ hours after
                        .RuleFor(s => s.EndDateTime, (f, s) => f.Date.Between(s.StartDateTime.Value, s.StartDateTime.Value.AddHours(10)))
                        .RuleFor(s => s.ShiftNotes, f => f.Lorem.Word())
                        .RuleFor(s => s.IsPublished, true)//by default all seeded shifts are published already 
                        .RuleFor(s => s.ApplicationUser, (f) => f.PickRandom(users))
                        .RuleFor(s => s.Site, (f, s) => s.ApplicationUser.Site);//chooses the site from the randomly assigned user

                    //generate 50 shifts
                    var shifts = faker.Generate(150);

                    foreach (var shift in shifts)
                    {
                        dbContext.Shifts.Add(shift);
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding shifts: {ErrorMessage}");

                //for debugging
                if (ex.InnerException != null)
                {
                    logger.LogError(ex.InnerException, "Inner Exception: {ErrorMessage}", ex.InnerException.Message);
                }
            }

        }

        public async Task SeedSite(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            var users = dbContext.Users.ToList();

            try
            {
                var sites = new List<Site>()
                {
                    new Site {
                        SiteName = "Merchant City",
                        MondayOpenTime = new DateTime(1, 1, 1, 12, 0, 0), // 12:00 PM
                        MondayCloseTime = new DateTime(1, 1, 1, 22, 0, 0), // 10:00 PM
                        TuesdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        TuesdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        WednesdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        WednesdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        ThursdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        ThursdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        FridayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        FridayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SaturdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        SaturdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SundayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        SundayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SiteConfiguration = new SiteConfiguration
                        {
                            NumberOfSections = 3,
                            CoversCapacity = 170,
                            BookingDuration = new TimeSpan(2, 15, 0),
                            RoleConfigurations = new List<RoleConfiguration>(),
                        }
                    },
                    new Site {
                        SiteName = "Southside",
                        MondayOpenTime = new DateTime(1, 1, 1, 12, 0, 0), // 12:00 PM
                        MondayCloseTime = new DateTime(1, 1, 1, 22, 0, 0), // 10:00 PM
                        TuesdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        TuesdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        WednesdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        WednesdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        ThursdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        ThursdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        FridayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        FridayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SaturdayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        SaturdayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SundayOpenTime = new DateTime(1, 1, 1, 12, 0, 0),
                        SundayCloseTime = new DateTime(1, 1, 1, 22, 0, 0),
                        SiteConfiguration = new SiteConfiguration
                        {
                            NumberOfSections = 2,
                            CoversCapacity = 125,
                            BookingDuration = new TimeSpan(2, 15, 0),
                            RoleConfigurations = new List<RoleConfiguration>()
                        }
                    },
                    new Site {SiteName = "No Site"}
                };

                foreach (var site in sites)
                {
                    if (!dbContext.Sites.Any(s => s.SiteName == site.SiteName))
                    {
                        dbContext.Sites.Add(site);
                    }
                }            

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding sites: {ErrorMessage}");

                //for debugging
                if (ex.InnerException != null)
                {
                    logger.LogError(ex.InnerException, "Inner Exception: {ErrorMessage}", ex.InnerException.Message);
                }
            }

        }

        public async Task SeedCompany(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var claimsService = services.GetRequiredService<IClaimsService>();

            var sites = dbContext.Sites.ToList();

            var userOwner = await SeedCompanyOwner(services);
            
            //if company  isnt found
            if (!dbContext.Companies.Any(c => c.CompanyName == "Six by Nico"))
            {
                var company = new Company()
                {
                    CompanyName = "Six by Nico",
                    ApplicationUserId = userOwner.Id
                };

                await dbContext.Companies.AddAsync(company);

                foreach (var site in sites)
                {
                    if (site.Company == null)
                    {
                        site.Company = company;
                    }
                }
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<ApplicationUser> SeedCompanyOwner(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser()
            {
                FirstName = "RyanOwner",
                LastName = "GrantOwner",
                Shifts = new List<Shift>(),
                TimeOffRequests = new List<TimeOffRequest>(),
                UserName = "owner@gmail.com",
                Email = "owner@gmail.com",
                NormalizedUserName = "OWNER@GMAIL.COM",
                NormalizedEmail = "OWNER@GMAIL.COM",
                EmailConfirmed = true,
                LockoutEnabled = true,
                PhoneNumberConfirmed = true,
                Salary = 15,
                ContractualHours = 45,
                Notes = "none"

            };         

            await userManager.CreateAsync(user, "sbn");
            await userManager.AddToRoleAsync(user, "Owner");
            await dbContext.SaveChangesAsync();

            //add claims
            var existingClaims = await userManager.GetClaimsAsync(user);

            // Add CompanyId claim if it doesn't exist
            if (!existingClaims.Any(c => c.Type == "CompanyId"))
            {
                await userManager.AddClaimAsync(user, new Claim("CompanyId", "1")); // Defaulting CompanyId to 1
            }


            return user;
        }

        public async Task SeedTimeOffRequests(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var users = dbContext.ApplicationUsers.ToList();
            try
            {
                if (!dbContext.TimeOffRequests.Any())
                {
                    //creating list of enums to apply to faker
                    var statusList = new List<ApprovedStatus>
                    {
                        ApprovedStatus.Pending,
                        ApprovedStatus.Approved,
                        ApprovedStatus.Denied

                    };

                    var faker = new Faker<TimeOffRequest>()
                        .RuleFor(t => t.Date, f => f.Date.Between(DateTime.Now.AddDays(-5), DateTime.Now.AddDays(7)))
                        .RuleFor(t => t.Notes, f => f.Lorem.Sentence())
                        .RuleFor(t => t.IsHoliday, f => f.Random.Bool())
                        .RuleFor(t => t.ApplicationUser, (f) => f.PickRandom(users))
                        .RuleFor(t => t.IsApproved, f => f.PickRandom(statusList));

                    //generate 20 time off requests
                    var timeOffRequests = faker.Generate(100);

                    foreach (var request in timeOffRequests)
                    {
                        dbContext.TimeOffRequests.Add(request);
                    }                 

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding sites: {ErrorMessage}");

                //for debugging
                if (ex.InnerException != null)
                {
                    logger.LogError(ex.InnerException, "Inner Exception: {ErrorMessage}", ex.InnerException.Message);
                }
            }
        }
    }
}
