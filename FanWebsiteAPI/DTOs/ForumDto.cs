using Fan_Website.Models;
using System.ComponentModel.DataAnnotations;

namespace Fan_Website.ViewModel
{
    public class ForumDto
    {
        public int ForumId { get; set; }
        [Required(ErrorMessage = "Please enter a title.")]
        public required string ForumTitle { get; set; }
        [Required(ErrorMessage = "Please enter a description.")]
        public required string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public int UserRating { get; set;  }
        public int PostsCount { get; set; }

        public string? Slug =>
            ForumTitle?.Replace(' ', '-').ToLower();
    }
}
