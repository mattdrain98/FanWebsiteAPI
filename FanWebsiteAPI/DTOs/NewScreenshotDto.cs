namespace FanWebsiteAPI.DTOs
{
    public class NewScreenshotDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public IFormFile? ImageFile { get; set; }
    }
}
