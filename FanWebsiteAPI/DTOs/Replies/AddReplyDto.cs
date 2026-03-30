namespace FanWebsiteAPI.DTOs.Replies
{
    public class AddReplyDto
    {
        public int PostId { get; set; }
        public string? ReplyContent { get; set; }
    }
}