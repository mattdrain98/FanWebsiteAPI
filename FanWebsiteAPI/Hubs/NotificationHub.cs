using FanWebsiteAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FanWebsiteAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly PresenceTracker _presence;

        public NotificationHub(PresenceTracker presence)
        {
            _presence = presence;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _presence.UserConnected(userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                _presence.UserDisconnected(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
