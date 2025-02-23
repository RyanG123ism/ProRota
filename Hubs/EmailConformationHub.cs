using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ProRota.Hubs
{
    public class EmailConfirmationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public async Task NotifyUser(string userId)
        {
            await Clients.User(userId).SendAsync("ReceiveConfirmation");
        }

        public async Task RegisterUser(string userId)
        {
            _connections[Context.ConnectionId] = userId;
            Console.WriteLine($"📌 SignalR Registered: {userId} -> {Context.ConnectionId}");
        }

        public static string? GetConnectionIdByUserId(string userId)
        {
            return _connections.FirstOrDefault(x => x.Value == userId).Key;
        }

        public override async Task OnConnectedAsync()
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                //maps the signalR connection to the user ID
                _connections[Context.ConnectionId] = userId;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            //removes the conneciton with the user ID
            _connections.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
