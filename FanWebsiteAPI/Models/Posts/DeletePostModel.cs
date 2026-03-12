namespace Fan_Website.Models.Posts
{
    public class DeletePostModel
    {
        public int PostId { get; set; }
        public required string PostAuthor { get; set; }
        public string? PostContent { get; set; }
        public int ForumId { get; set; }
    }
}
