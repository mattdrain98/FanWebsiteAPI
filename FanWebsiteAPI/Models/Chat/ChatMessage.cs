namespace FanWebsiteAPI.Models.Chat
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ForumId { get; set; }
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string? UserImagePath { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
