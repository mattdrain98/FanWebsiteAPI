namespace FanWebsiteAPI.DTOs
{
    public class AddForumDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
    }
}