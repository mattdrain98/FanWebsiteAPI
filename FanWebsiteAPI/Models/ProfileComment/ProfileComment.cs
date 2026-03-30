namespace Fan_Website.Models.ProfileComment
{
    public class ProfileComment
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime UpdatedOn { get; set; }
        public required ApplicationUser ProfileUser { get; set; }
        public required ApplicationUser CommentUser { get; set; }
    }
}
