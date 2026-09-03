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

        // Denormalized reply-to snapshot, captured at send time (same pattern as
        // UserName/UserImagePath above) so a quoted preview still renders correctly
        // even if the original message is later deleted or its author changes name.
        public int? ReplyToMessageId { get; set; }
        public string? ReplyToUserName { get; set; }
        public string? ReplyToContent { get; set; }
    }
}
