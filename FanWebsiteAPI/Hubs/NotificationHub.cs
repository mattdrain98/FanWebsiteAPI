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

        // This connection is established for the app's whole foreground lifetime (see
        // NotificationProvider on the client), not just while viewing a specific forum's
        // chat — makes it the right signal for general "is this user online" presence.
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
