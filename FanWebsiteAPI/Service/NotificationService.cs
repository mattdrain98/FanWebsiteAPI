using Fan_Website;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Models.Notification;

namespace FanWebsiteAPI.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
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
        }
    }
}
