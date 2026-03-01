namespace FanWebsiteAPI.DTOs
{
    public class PostReplyDto
    {
        public int PostId { get; set; }
        public required string PostTitle { get; set; }
        public required string PostContent { get; set; } 
        public string? ReplyContent { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public string? AuthorImageUrl { get; set; }
        public int AuthorRating { get; set; }
        public DateTime Date { get; set; }
        public int ForumId { get; set; }
        public required string ForumName { get; set; }
    }
}
