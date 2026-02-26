namespace Fan_Website
{
    public class PostReply
    {
        public required int Id { get; set; }
        public required string Content { get; set; }
        public required DateTime CreateOn { get; set; }
        public required ApplicationUser User { get; set; }
        public required Post Post { get; set; }
    }
}
