namespace Fan_Website.Models.Posts
{
    public class PostTopicModel
    {
        public IEnumerable<PostListingModel>? Posts { get; set; }
        public string? SearchQuery { get; set; }
    }
}
