using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using ProRota.Models;
using ProRota.Services;
using System.Diagnostics;

namespace ProRota.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICompanyService _companyService;

        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ICompanyService companyService)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _companyService = companyService;
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
            return View();
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
