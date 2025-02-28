using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using ProRota.Hubs;
using ProRota.Services;

namespace ProRota.Controllers
{
    public class NewsFeedController : Controller
    {
        private readonly IHubContext<NewsFeedHub> _hubContext;
        private readonly INewsFeedService _newsFeedService;

        public NewsFeedController(IHubContext<NewsFeedHub> hubContext, INewsFeedService newsFeedService)
        {
            _hubContext = hubContext;
            _newsFeedService = newsFeedService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewsFeedItem(int siteId, string newsMessage, bool companyWideCheckbox = false)
        {
            if(siteId == 0)
            {
                throw new Exception("Can't find site Id to create new post");
            }

            if(companyWideCheckbox == null)
            {
                throw new Exception("Company wide check box == null");
            }

            if(newsMessage.IsNullOrEmpty() || newsMessage.Length == 0 || newsMessage.Length <= 5)
            {
                ViewBag.Error = "A post must be more than 5 charecters";
                return RedirectToAction("Home", "Home");
            }

            if(companyWideCheckbox)
            {
                //company wide post
                var post = await _newsFeedService.createAndPostNewsFeedItem(newsMessage);
            }
            else
            {
                //site wide post
                var post = await _newsFeedService.createAndPostNewsFeedItem(newsMessage, siteId);
            }

            //back to home page
            return RedirectToAction("Home", "Home");
        }
    }
}
