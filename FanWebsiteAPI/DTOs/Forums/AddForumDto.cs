namespace FanWebsiteAPI.DTOs.Forums
{
    public class AddForumDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
    }
}