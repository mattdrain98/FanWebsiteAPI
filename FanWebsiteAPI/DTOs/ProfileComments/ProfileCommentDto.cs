using FanWebsiteAPI.Infrastructure.Abstractions;

namespace FanWebsiteAPI.DTOs.ProfileComments
{
    public class ProfileCommentDto : AuthorDto
    {
        public int Id { get; set; }                   
        public string? CommentContent { get; set;  }
        public required string ProfileUserId { get; set; }           
        public required string ProfileUserName { get; set; }       
        public string? ProfileUserImageUrl { get; set; }  
        public int ProfileUserRating { get; set; }       
    }
}
