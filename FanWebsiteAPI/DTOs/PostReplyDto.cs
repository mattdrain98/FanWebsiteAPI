using FanWebsiteAPI.Infrastructure.Abstractions;

namespace FanWebsiteAPI.DTOs
{
    public class PostReplyDto : AuthorDto
    {
        public int PostId { get; set; }
        public required string PostTitle { get; set; }
        public required string PostContent { get; set; } 
        public string? ReplyContent { get; set; }
        public int ForumId { get; set; }
        public required string ForumName { get; set; }
    }
}
