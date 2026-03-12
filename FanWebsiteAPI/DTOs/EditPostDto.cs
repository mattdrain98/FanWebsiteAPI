namespace FanWebsiteAPI.DTOs
{
    public class EditPostDto
    {
        public required string Title { get; set; }
        public string? Content { get; set; }
        public List<string>? NewImageUrls { get; set; }
    }
}
