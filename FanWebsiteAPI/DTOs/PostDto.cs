using FanWebsiteAPI.Infrastructure.Abstractions;

namespace Fan_Website.ViewModel
{
    public class CreatePostRequest
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public int ForumId { get; set; }
        public List<string>? ImageUrls { get; set; } = new();
    }

    public class PostImageDto
    {
        public int Id { get; set; }
        public required string Url { get; set; }
    }

    public class PostDto : AuthorDto
    {
        public int PostId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ForumId { get; set; }
        public string? ForumName { get; set; }
        public int TotalLikes { get; set; }
        public int RepliesCount { get; set; }
        public List<PostImageDto>? PostImages { get; set; }
    }
}
