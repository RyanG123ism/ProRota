using Microsoft.AspNetCore.Mvc;
using ProRota.Data;
using SQLitePCL;

namespace ProRota.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {

        private ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) 
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            var sites = _context.Sites.ToList();
            ViewBag.Sites = sites;

            return View();
        }

    }
}
