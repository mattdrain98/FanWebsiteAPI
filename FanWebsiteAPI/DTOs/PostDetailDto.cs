using Fan_Website.Models.Reply;
using Fan_Website.ViewModel;

namespace FanWebsiteAPI.DTOs
{
    public class PostDetailDto : PostDto
    {
        public IEnumerable<PostReplyModel>? Replies { get; set; }
        public IEnumerable<LikeDto>? Likes { get; set; }
        public bool UserHasLiked { get; set; }
    }
}
