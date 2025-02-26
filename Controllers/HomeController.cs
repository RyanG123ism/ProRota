using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using SQLitePCL;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;

namespace ProRota.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICompanyService _companyService;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ICompanyService companyService, ApplicationDbContext context)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _companyService = companyService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
             
            //checks is user is logged in
            if(_signInManager.IsSignedIn(User))
            {
                if(User.IsInRole("Partial_User_Unpaid"))//unpaid account
                {
                    return View("Services");
                }
                else if(User.IsInRole("Partial_User_Paid"))//paid account - still to set up company account
                {
                    return RedirectToAction("CreateCompany", "Company", new { area = "Admin" });
                }
                else
                {
                    if (TempData["InitialLogin"] != null)
                    {
                        if ((bool)TempData["InitialLogin"] == true)
                        {
                            //create session for initial login (once account ans company fully created)
                            await _companyService.CreateSession();
                            TempData.Remove("InitialLogin");//drop data
                        }
                    }

                    return RedirectToAction("Home");//fully set up account - goes to normal home page
                }  
            }

            //otherwise return the welcomepage / index
            return View();
        }

        public IActionResult Home()
        {
            var User = HttpContext.User;
            //get user to pass into model
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var siteIdClaim = User.FindFirst("SiteId")?.Value;

            if (string.IsNullOrEmpty(companyIdClaim) || string.IsNullOrEmpty(siteIdClaim))
            {
                return View(new List<NewsFeedItem>());
                //throw new Exception("Cannot find user claims");
            }
            else
            {
                var companyParse = int.TryParse(companyIdClaim, out int companyId);
                var siteClaim = int.TryParse(siteIdClaim, out int siteId);

                if(companyParse && siteClaim)
                {
                    var newsFeed = _context.NewsFeedItems
                        .Where(n =>
                                (n.TargetType == NewsFeedTargetType.User && n.ApplicationUserId == userId) ||
                                (n.TargetType == NewsFeedTargetType.Site && n.SiteId == siteId) ||
                                (n.TargetType == NewsFeedTargetType.Company && n.CompanyId == companyId))
                            .OrderByDescending(n => n.Timestamp)
                            .ToList();

                    return View(newsFeed);
                }

                throw new Exception("Cannot parse user claims");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }


        public IActionResult About()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
