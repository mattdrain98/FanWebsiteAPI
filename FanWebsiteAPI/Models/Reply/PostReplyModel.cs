namespace Fan_Website.Models.Reply
{
    public class PostReplyModel
    {
        public int Id { get; set; }

        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required int AuthorRating { get; set; }
        public string? AuthorImageUrl { get; set; }
        public required DateTime Date { get; set; }
        public required string ReplyContent { get; set; }

        public required int PostId { get; set; }
        public required string PostTitle { get; set; }
        public required string PostContent { get; set; }

        public required string ForumName { get; set; }
        public required int ForumId { get; set; }
    }
}
