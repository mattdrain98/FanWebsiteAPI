namespace Fan_Website
{
    public class DeletePostReply
    {
        public int Id { get; set; }
        public required string ReplyContent { get; set; }
        public required Post Post { get; set; }
    }
}
