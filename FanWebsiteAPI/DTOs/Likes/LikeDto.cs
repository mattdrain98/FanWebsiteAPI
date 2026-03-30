namespace FanWebsiteAPI.DTOs.Likes
{
    public class LikeDto
    {
        public int Id { get; set; }

        public required LikeUserDto User { get; set; }
    }
}
