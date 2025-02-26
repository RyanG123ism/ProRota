using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProRota.Hubs;

namespace ProRota.Controllers
{
    public class NewsFeedController : Controller
    {
        private readonly IHubContext<NewsFeedHub> _hubContext;

        public NewsFeedController(IHubContext<NewsFeedHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> PostNews(string message)
        {
            // Here you would save the news to the database (omitted for brevity)

            // Notify all users
            await _hubContext.Clients.All.SendAsync("ReceiveNewsUpdate", message);

            return Ok(new { Message = "News update sent!" });
        }
    }
}
