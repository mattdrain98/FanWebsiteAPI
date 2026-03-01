namespace FanWebsiteAPI.DTOs
{
    public class AddReplyDto
    {
        public int PostId { get; set; }
        public string? ReplyContent { get; set; }
    }
}
