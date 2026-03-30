using FanWebsiteAPI.Infrastructure.Abstractions;

namespace Fan_Website.Models.Forum
{
    public class ForumListingModel : AuthorDto
    {
        public int ForumId { get; set; }
        public required string ForumTitle { get; set; }
        public required string Description { get; set; }
    }
}
