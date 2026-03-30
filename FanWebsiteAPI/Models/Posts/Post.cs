using FanWebsiteAPI.Models.Posts;
using System.ComponentModel.DataAnnotations;

namespace Fan_Website
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }
        [Required(ErrorMessage = "Please enter a title.")]
        public required string Title { get; set; }
        [Required(ErrorMessage = "Please enter content.")]
        public required string Content { get; set; } 
        public DateTime UpdatedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public int ForumId { get; set; }
        public required Forum Forum { get; set; }
        public required List<PostReply> Replies { get; set; }
        public int TotalLikes { get; set; }
        public required List<Like> Likes { get; set; }
        public List<PostImage>? PostImages { get; set; }
        public string? Slug =>
            Title?.Replace(' ', '-').ToLower();

    }
}
