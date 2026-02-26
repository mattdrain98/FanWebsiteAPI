namespace Fan_Website.Models.Screenshot
{
    public class AddScreenshotModel
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required IFormFile ImageFile { get; set; }
    }
}
