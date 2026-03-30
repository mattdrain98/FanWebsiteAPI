namespace Fan_Website
{
    public class PostReply
    {
        public int Id { get; set; }
        public required string ReplyContent { get; set; }
        public required DateTime UpdatedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public required Post Post { get; set; }
    }
}
