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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IClaimsService _claimsService;

        public HomeController(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IClaimsService claimsService)
        {
            _signInManager = signInManager;
            _context = context;
            _claimsService = claimsService;
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
                    return RedirectToAction("Home");//fully set up account - goes to normal home page
                }  
            }

            //otherwise return the welcomepage / index
            return View();
        }

        public async Task<IActionResult> Home()
        {
            var userId = _claimsService.GetUserId();

            var companyId = _claimsService.GetCompanyId();
            var siteId = _claimsService.GetSiteId();

            if(companyId == 0 && siteId == 0)
            {
                return View(new List<NewsFeedItem>());
            }
            else
            {
                var newsFeed = await _context.NewsFeedItems
                        .Where(n =>
                                (n.TargetType == NewsFeedTargetType.User && n.ApplicationUserId == userId) ||
                                (n.TargetType == NewsFeedTargetType.Site && n.SiteId == siteId) ||
                                (n.TargetType == NewsFeedTargetType.Company && n.CompanyId == companyId))
                            .OrderByDescending(n => n.Timestamp)
                            .Include(n => n.CreatedByUser)//include author
                            .ToListAsync();

                return View(newsFeed);
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
