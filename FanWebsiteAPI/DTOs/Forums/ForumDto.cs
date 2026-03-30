using Fan_Website.Models;
using FanWebsiteAPI.Infrastructure.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace FanWebsiteAPI.DTOs.Forums
{
    public class ForumDto : AuthorDto
    {
        public int ForumId { get; set; }
        [Required(ErrorMessage = "Please enter a title.")]
        public required string ForumTitle { get; set; }
        [Required(ErrorMessage = "Please enter a description.")]
        public required string Description { get; set; }
        public int PostsCount { get; set; }

        public string? Slug =>
            ForumTitle?.Replace(' ', '-').ToLower();
    }
}
