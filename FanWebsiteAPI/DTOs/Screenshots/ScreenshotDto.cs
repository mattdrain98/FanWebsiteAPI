using FanWebsiteAPI.Infrastructure.Abstractions;

namespace FanWebsiteAPI.DTOs.Screenshots
{
    public class ScreenshotDto : AuthorDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}
