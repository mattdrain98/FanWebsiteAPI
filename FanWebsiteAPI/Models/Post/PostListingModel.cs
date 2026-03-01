using Fan_Website.Models.Forum;

namespace Fan_Website.Models
{
    public class PostListingModel
    {
        public ForumListingModel? Forum { get; set; }

        public int Id { get; set; }
        public required string Title { get; set; }
        public required string AuthorName { get; set; }
        public int AuthorRating { get; set; }
        public required string AuthorId { get; set; }
        public required string DatePosted { get; set; }
        public int TotalLikes { get; set; }
        public int ForumId { get; set; }
        public string? ForumName { get; set; }

        public int RepliesCount { get; set; }
    }
}
