namespace FanWebsiteAPI.DTOs.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public required string Message { get; set; }
        public required string Link { get; set; }
        public bool IsRead { get; set; }
        public required string Type { get; set; }
        public required string CreatedOn { get; set; }
    }
}
