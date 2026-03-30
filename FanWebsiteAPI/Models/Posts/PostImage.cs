using Fan_Website;

namespace FanWebsiteAPI.Models.Posts
{
    public class PostImage
    {
        public int Id { get; set; }
        public required string Url { get; set; }
        public required DateTime UpdatedOn { get; set; }    

        public int PostId { get; set; } 
        public required Post Post { get; set; }
    }
}
