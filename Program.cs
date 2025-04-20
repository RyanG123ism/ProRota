using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using ProRota.Data;
using ProRota.Hubs;
using ProRota.Models;
using ProRota.Services;
using Rotativa.AspNetCore;
using Stripe;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString == null)
{
    Console.WriteLine("Connection string not found.");
    throw new InvalidOperationException("Missing DefaultConnection string.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));



builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    //password configuration - CHANGE THIS BACK TO NORMAL BEFORE SUBMITTING
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 0;

}).AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();



builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddRazorPages();

builder.Services.AddSignalR();//Register SignalR

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true; // Optional: Makes JSON more readable
    });

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue; // Allows larger request sizes
});


// Add session services
builder.Services.AddDistributedMemoryCache(); // Required for session storage
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); // Set session timeout
    options.Cookie.HttpOnly = true; // Make the cookie accessible only by the server
    options.Cookie.IsEssential = true; // Required for GDPR compliance
});


//adding service classes and interfaces
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IRotaService, RotaService>();
builder.Services.AddScoped<IAlgorithmService, AlgorithmService>();
builder.Services.AddScoped<ITimeOffRequestService, TimeOffRequestService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<StripePaymentService>(); // stripe service
builder.Services.AddSingleton<IExtendedEmailSender, EmailSenderService>(); //email service
builder.Services.AddScoped<INewsFeedService, NewsFeedService>();
builder.Services.AddSingleton<EmailConfirmationHub>();
builder.Services.AddSingleton<NewsFeedHub>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//adding stripe 
var stripeSettings = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

//use session
app.UseSession();

//configuring rotativa for PDF downloads
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

//get the scope factory
using var scope = app.Services.CreateScope();

//get the service provider
var serviceProvider = scope.ServiceProvider;
try
{
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.Migrate();
        var databaseInitialiser = new DbInitialiser(serviceProvider);
        await databaseInitialiser.Seed();

        app.UseMigrationsEndPoint();
    }
    else
    {
        dbContext.Database.Migrate();
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error during database setup or migration");
    Console.WriteLine(ex.ToString());
}



////delete the existing database and create a new oneee
//dbContext.Database.EnsureDeleted();
////dbContext.Database.EnsureCreated();

//// Apply migrations
//dbContext.Database.Migrate();

//// Create a new DatabaseInitialiser
//var databaseInitialiser = new DbInitialiser(serviceProvider);//COMMENT THIS OUT WHEN APP GOES LIVE

//// Seed the database
//await databaseInitialiser.Seed();

//local DB
//"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProRota_v3;Trusted_Connection=True;MultipleActiveResultSets=true",



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
app.MapHub<EmailConfirmationHub>("/emailConfirmationHub");
app.MapHub<NewsFeedHub>("/newsFeedHub");

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Application failed to start!");
    Console.WriteLine(ex.ToString());
    throw;
}
