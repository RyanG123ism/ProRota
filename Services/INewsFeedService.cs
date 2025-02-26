using Microsoft.AspNetCore.Mvc;

namespace ProRota.Services
{
    public interface INewsFeedService
    {
        public Task<IActionResult> createAndPostNewsFeedItem(string message);
        public Task<IActionResult> createAndPostNewsFeedItem(string message, int siteId);
        public Task<IActionResult> createAndPostNewsFeedItem(string message, string recipientId);
    }
}
