using Fan_Website;
using FanWebsiteAPI.Hubs;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Models.Notification;
using Microsoft.AspNetCore.SignalR;

namespace FanWebsiteAPI.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(string userId, string message, string type, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                Link = link,
                IsRead = false,
                CreatedOn = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Message,
                notification.Type,
                notification.Link,
                notification.IsRead,
                CreatedOn = notification.CreatedOn.ToString("o")
            });
        }
    }
}
