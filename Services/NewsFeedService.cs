using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using ProRota.Data;
using ProRota.Hubs;
using ProRota.Models;
using System.Security.Claims;

namespace ProRota.Services
{
    public class NewsFeedService : INewsFeedService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NewsFeedHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;//accessing httpContext properties of controller base

        public NewsFeedService(IHubContext<NewsFeedHub> hubContext, ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
            _context = dbContext;
        }

        public async Task<IActionResult> PostMessage(string message)
        {
            // Here you would save the news to the database (omitted for brevity)

            // Notify all users
            await _hubContext.Clients.All.SendAsync("ReceiveNewsUpdate", message);

            return null;
        }

        public async Task<IActionResult> createAndPostNewsFeedItem(string message)
        {
            if (message.IsNullOrEmpty())
            {
                return null;
            }
            //create newsFeedObject here
            var senderId = getSenderId();
            var companyId = getCompanyId();

            if (senderId != null && companyId != 0)
            {
                //create timeFeed object
                var newsFeedItem = new NewsFeedItem
                {
                    CreatedByUserId = senderId,
                    Message = message,
                    TargetType = NewsFeedTargetType.Company,
                    CompanyId = companyId
                };

                //save to DB
                await _context.NewsFeedItems.AddAsync(newsFeedItem);
                await _context.SaveChangesAsync();

                //send alert with signal R
                await _hubContext.Clients.Group($"Company_{companyId}").SendAsync("ReceiveNewsUpdate");
            }
            return null;
            return null;

        }
        public async Task<IActionResult> createAndPostNewsFeedItem(string message, string receipientId)
        {

            if(message.IsNullOrEmpty() || receipientId.IsNullOrEmpty())
            {
                return null;
            }
            //create newsFeedObject here
            var senderId = getSenderId();

            if(senderId != null)
            {
                //create timeFeed object
                var newsFeedItem = new NewsFeedItem { 
                    CreatedByUserId = senderId,
                    ApplicationUserId = receipientId,
                    Message = message,
                    TargetType = NewsFeedTargetType.User,
                };

                //save to DB
                await _context.NewsFeedItems.AddAsync(newsFeedItem);
                await _context.SaveChangesAsync();

                //send alert with signal R
                await _hubContext.Clients.User(receipientId).SendAsync("ReceiveNewsUpdate");
            }
            return null;
        }

        public async Task<IActionResult> createAndPostNewsFeedItem(string message, int siteId)
        {
            if (message.IsNullOrEmpty() || siteId == 0)
            {
                return null;
            }
            //create newsFeedObject here
            var senderId = getSenderId();

            if (senderId != null)
            {
                //create timeFeed object
                var newsFeedItem = new NewsFeedItem
                {
                    CreatedByUserId = senderId,
                    SiteId = siteId,
                    Message = message,
                    TargetType = NewsFeedTargetType.Site,
                };

                //save to DB
                await _context.NewsFeedItems.AddAsync(newsFeedItem);
                await _context.SaveChangesAsync();

                //send alert with signal R
                await _hubContext.Clients.Group($"Site_{siteId}").SendAsync("ReceiveNewsUpdate");
            }
            return null;

        }

        public string getSenderId()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if(httpContext != null)
            {
                var senderId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(senderId))
                {
                    return string.Empty; //return empty string
                }

                return senderId;
            }
            else
            {
                return string.Empty;
            }

        }

        public int getCompanyId()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var companyIdClaim = httpContext.User.FindFirst("CompanyId")?.Value;

            if (string.IsNullOrEmpty(companyIdClaim))
            {
                return 0; // Company ID is missing
            }

            var parse = int.TryParse(companyIdClaim, out int id);

            if(parse)
            {
                return id;
            }

            return 0;
        }


    }
}

    