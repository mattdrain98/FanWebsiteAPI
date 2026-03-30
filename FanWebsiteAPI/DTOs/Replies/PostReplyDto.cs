using FanWebsiteAPI.Infrastructure.Abstractions;

namespace FanWebsiteAPI.DTOs.Replies
{
    public class PostReplyDto : AuthorDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string? ReplyContent { get; set; }
    }
}
