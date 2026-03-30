using FanWebsiteAPI.DTOs.Forums;
using FanWebsiteAPI.DTOs.Posts;

namespace FanWebsiteAPI.DTOs.Search
{
    public class SearchResultDto
    {
        public IEnumerable<PostDto>? Posts { get; set; }
        public string? SearchQuery { get; set; }
        public bool EmptySearchResults { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalPosts { get; set; }
        public ForumDto? Forum { get; set; } 
    }
}