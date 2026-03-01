namespace FanWebsiteAPI.DTOs
{
    public class PostReplyDto
    {
        public int PostId { get; set; }
        public string PostTitle { get; set; } = null!;
        public string PostContent { get; set; } = null!;
        public string ReplyContent { get; set; } = null!; // For POST
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string AuthorImageUrl { get; set; } = null!;
        public int AuthorRating { get; set; }
        public DateTime Date { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; } = null!;
    }
}
