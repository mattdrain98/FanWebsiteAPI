namespace FanWebsiteAPI.Models.Chat
{
    // A user's opt-in membership in a forum's chat, established via the "Join" action.
    // Used to scope chat notifications to only the users actually participating in a
    // given forum's chat, instead of broadcasting to everyone.
    public class ChatParticipant
    {
        public int Id { get; set; }
        public int ForumId { get; set; }
        public required string UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
