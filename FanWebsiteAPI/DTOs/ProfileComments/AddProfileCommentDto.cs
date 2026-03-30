namespace FanWebsiteAPI.DTOs.ProfileComments
{
    public class AddProfileCommentDto
    {
        public required string ProfileUserId { get; set; }
        public required string CommentContent { get; set; }
    }

    public class EditProfileCommentDto
    {
        public required string CommentContent { get; set; }
    }
}
