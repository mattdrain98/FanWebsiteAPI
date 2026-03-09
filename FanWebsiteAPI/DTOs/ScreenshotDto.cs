namespace FanWebsiteAPI.DTOs
{
    public class ScreenshotDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public int AuthorRating { get; set; }
        public DateTime DatePosted { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}
