using Fan_Website.ViewModel;

namespace FanWebsiteAPI.DTOs
{
    public class HomeIndexDto
    {
        public required IEnumerable<PostDto> LatestPosts { get; set; }
        public string? SearchQuery { get; set; }
    }
}
