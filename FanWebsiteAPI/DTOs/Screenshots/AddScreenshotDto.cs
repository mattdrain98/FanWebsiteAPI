namespace FanWebsiteAPI.DTOs.Screenshots
{
    public class AddScreenshotDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public IFormFile? ImageFile { get; set; }
    }
}
