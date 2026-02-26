namespace Fan_Website
{
    public class DeletePostReply
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public required Post Post { get; set; }
    }
}
