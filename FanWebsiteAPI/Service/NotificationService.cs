using Fan_Website;
using FanWebsiteAPI.Hubs;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Models.Notification;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

namespace FanWebsiteAPI.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
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

            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Message,
                notification.Type,
                notification.Link,
                notification.IsRead,
                CreatedOn = notification.CreatedOn.ToString("o")
            });

            // Send Expo push notification for background delivery
            var user = await _context.Users.FindAsync(userId);
            var pushToken = user?.ExpoPushToken;
            if (!string.IsNullOrEmpty(pushToken))
            {
                try
                {
                    var payload = new
                    {
                        to = pushToken,
                        title = "Dismino",
                        body = message,
                        data = new { type, link },
                        sound = "default"
                    };
                    var http = _httpClientFactory.CreateClient();
                    var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"
                    );
                    await http.PostAsync("https://exp.host/api/v2/push/send", content);
                }
                catch { }
            }
        }
    }
}
