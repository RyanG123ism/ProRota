using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ProRota.Hubs
{
    public class NewsFeedHub : Hub
    {
        public async Task SendNewsUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveNewsUpdate", message);
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var siteId = user.FindFirst("SiteId")?.Value;
            var companyId = user.FindFirst("CompanyId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                //add the user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            if (!string.IsNullOrEmpty(siteId))
            {
                //add the user to their site's group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Site_{siteId}");
            }

            if (!string.IsNullOrEmpty(companyId))
            {
                //add the user to their company's group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
            }

            await base.OnConnectedAsync();
        }

    }
}
