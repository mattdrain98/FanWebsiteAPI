using FanWebsiteAPI.DTOs.Likes;
using FanWebsiteAPI.DTOs.Replies;

namespace FanWebsiteAPI.DTOs.Posts
{
    public class PostDetailDto : PostDto
    {
        public IEnumerable<PostReplyDto>? Replies { get; set; }
        public IEnumerable<LikeDto>? Likes { get; set; }
        public bool UserHasLiked { get; set; }
    }
}
