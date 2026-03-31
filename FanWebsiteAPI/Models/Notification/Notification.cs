using Fan_Website;

namespace FanWebsiteAPI.Models.Notification
{
    public class Notification
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public required string Message { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Type { get; set; } = "general"; // like, reply, follow, comment
    }
}
