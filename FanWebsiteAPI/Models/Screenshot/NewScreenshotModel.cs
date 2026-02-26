namespace Fan_Website.Models.Screenshot
{
    public class NewScreenshotModel
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorRating { get; set; }
        public required string ScreenshotImageUrl { get; set; }
        public required IFormFile ScreenshotUpload { get; set; }
    }
}
