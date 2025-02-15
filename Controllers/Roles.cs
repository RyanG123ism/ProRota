using Microsoft.AspNetCore.Mvc;

namespace ProRota.Controllers
{
    public class Roles : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
