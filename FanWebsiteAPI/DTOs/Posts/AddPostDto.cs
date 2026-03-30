namespace FanWebsiteAPI.DTOs.Posts
{
    public class AddPostDto
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public int ForumId { get; set; }
        public List<string>? ImageUrls { get; set; } = new();
    }

    public class EditPostDto
    {
        public required string Title { get; set; }
        public string? Content { get; set; }
        public List<string>? NewImageUrls { get; set; }
    }
}
