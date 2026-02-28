namespace FanWebsiteAPI.DTOs
{
    public class LikeDto
    {
        public int Id { get; set; }

        public LikeUserDto User { get; set; }
    }

    public class LikeUserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
    }
}
