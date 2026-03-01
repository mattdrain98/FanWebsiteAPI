using Fan_Website.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.ViewModel
{
    public class PostDto
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public int ForumId { get; set; }
        public required Forum Forum { get; set; }
        public IEnumerable<PostReply>? Replies { get; set; }
        public int TotalLikes { get; set; }
        public List<Like>? Likes { get; set; }
        public string? Slug =>
            Title?.Replace(' ', '-').ToLower();
    }
}
