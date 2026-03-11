using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;
using Microsoft.AspNetCore.Identity;

namespace Fan_Website
{
    public class ApplicationUser: IdentityUser
    {
        public int Rating { get; set; }
        public string? ImagePath { get; set; }
        public DateTime MemberSince { get; set; }
        public bool IsActive { get; set; }
        public int Following { get; set; }
        public int Followers { get; set; }
        public List<Follow> Follows { get; set; } = new List<Follow>(); 
        public List<Follow> Followings { get; set; } = new List<Follow>();
        public List<ProfileComment> ProfileComments { get; set; } = new List<ProfileComment>(); // comments ON this profile
        public List<ProfileComment> CommentsMade { get; set; } = new List<ProfileComment>();      // comments this user has written
        public string? Bio { get; set; }
    }
}
