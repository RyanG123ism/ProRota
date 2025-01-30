using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProRota.Models;
using System.Diagnostics;

namespace ProRota.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private SignInManager<ApplicationUser> _signInManager;

        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            //will return the home page if a user is signed in
            if(_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Home");
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
