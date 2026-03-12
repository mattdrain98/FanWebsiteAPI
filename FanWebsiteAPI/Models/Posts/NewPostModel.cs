namespace Fan_Website.Models.Posts
{
    public class NewPostModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int ForumId { get; set; }
        public string? Content { get; set; }
        public DateTime Created { get; set; }
    }
}
