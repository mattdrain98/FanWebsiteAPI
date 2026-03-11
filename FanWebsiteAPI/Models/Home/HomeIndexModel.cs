namespace Fan_Website.Models.Home
{
    public class HomeIndexModel
    {
        public string? SearchQuery { get; set; }
        public IEnumerable<PostListingModel>? LatestPosts { get; set; }
    }
}
