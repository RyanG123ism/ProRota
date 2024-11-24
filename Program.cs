using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{ 
    options.SignIn.RequireConfirmedAccount = true;
    //password configuration - CHANGE THIS BACK TO NORMAL BEFORE SUBMITTING
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 0;

}).AddRoles<IdentityRole>()//just added
  .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorPages();

//builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Get the scope factory
using var scope = app.Services.CreateScope();

// Get the service provider
var serviceProvider = scope.ServiceProvider;

// Get the DbContext
var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

// Delete the existing database and create a new one
dbContext.Database.EnsureDeleted();
//dbContext.Database.EnsureCreated();

// Apply migrations
dbContext.Database.Migrate();

// Create a new DatabaseInitialiser
var databaseInitialiser = new DbInitialiser(serviceProvider);

// Seed the database
await databaseInitialiser.Seed();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();



app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
