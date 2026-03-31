namespace FanWebsiteAPI.Infrastructure
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string message, string type, string? link = null);
    }
}
