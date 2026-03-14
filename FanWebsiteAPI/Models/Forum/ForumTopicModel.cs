using System.Collections.Generic;

namespace Fan_Website.Models.Forum
{
    public class ForumTopicModel
    {
        public required ForumListingModel Forum { get; set; }
        public IEnumerable<PostListingModel>? Posts { get; set; }
        public string? SearchQuery { get; set; }
    }
}
