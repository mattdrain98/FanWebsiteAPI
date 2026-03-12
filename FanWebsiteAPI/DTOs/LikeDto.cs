namespace FanWebsiteAPI.DTOs
{
    public class LikeDto
    {
        public int Id { get; set; }

        public required LikeUserDto User { get; set; }
    }

    public class LikeUserDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
    }
}
