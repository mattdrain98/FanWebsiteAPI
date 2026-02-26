using System.Collections.Generic;

namespace Fan_Website.Models.Post
{
    public class PostTopicModel
    {
        public IEnumerable<PostListingModel> Posts { get; set; }
        public string SearchQuery { get; set; }
    }
}
