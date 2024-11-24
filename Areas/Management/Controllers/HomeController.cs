using Microsoft.AspNetCore.Mvc;

namespace ProRota.Areas.Management.Controllers
{
    public class HomeController : Controller
    {
        [Area("Management")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
