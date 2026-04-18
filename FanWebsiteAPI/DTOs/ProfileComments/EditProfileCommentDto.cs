namespace FanWebsiteAPI.DTOs.ProfileComments
{
    public class EditProfileCommentDto
    {
        public int Id { get; set; }
        public required string CommentContent { get; set; }
    }
}
