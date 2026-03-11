namespace Fan_Website
{
    public class Like
    {
        public int Id { get; set; }
        public required ApplicationUser User { get; set; }
        public required Post Post { get; set; }
    }
}
