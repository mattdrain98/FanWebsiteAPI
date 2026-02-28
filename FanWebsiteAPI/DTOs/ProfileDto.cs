namespace FanWebsiteAPI.DTOs
{
    public class ProfileDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserRating { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string MemberSince { get; set; }
        public int Following { get; set; }
        public int Followers { get; set; }
        public IEnumerable<FollowDto> Follows { get; set; }      // Users this person follows
        public IEnumerable<FollowDto> Followings { get; set; }  // Users following this person
        public IEnumerable<ProfileCommentDto> ProfileComments { get; set; }
        public string? Bio { get; set; }
    }
}
