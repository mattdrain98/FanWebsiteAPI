using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Models.Search;
using Fan_Website.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IPost postService;

        public SearchController(IPost _postService)
        {
            postService = _postService;
        }

        // GET: api/Search?query=keyword
        [HttpGet]
        public IActionResult Results([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { Message = "Search query cannot be empty." });

            var posts = postService.GetFilteredPosts(query).ToList();
            var areNoResults = !posts.Any();

            var postListings = posts.Select(post => new PostListingModel
            {
                Id = post.PostId,
                Title = post.Title,
                AuthorId = post.User.Id,
                AuthorName = post.User.UserName ?? "Unknown",
                AuthorRating = post.User.Rating,
                AuthorUrl = post.User.ImagePath,
                DatePosted = post.CreatedOn.ToString(),
                RepliesCount = post.Replies?.Count() ?? 0,
                Forum = BuildForumListing(post)
            });

            var result = new SearchResultModel
            {
                Posts = postListings,
                SearchQuery = query,
                EmptySearchResults = areNoResults
            };

            return Ok(result);
        }

        // POST: api/Search
        [HttpPost]
        public IActionResult Search([FromBody] SearchRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { Message = "Search query cannot be empty." });

            var posts = postService.GetFilteredPosts(request.Query).ToList();
            var areNoResults = !posts.Any();

            var postListings = posts.Select(post => new PostListingModel
            {
                Id = post.PostId,
                Title = post.Title,
                AuthorId = post.User.Id,
                AuthorName = post.User.UserName ?? "Unknown",
                AuthorRating = post.User.Rating,
                AuthorUrl = post.User.ImagePath, 
                DatePosted = post.CreatedOn.ToString(),
                RepliesCount = post.Replies?.Count() ?? 0,
                Forum = BuildForumListing(post)
            });

            var result = new SearchResultModel
            {
                Posts = postListings,
                SearchQuery = request.Query,
                EmptySearchResults = areNoResults
            };

            return Ok(result);
        }

        // Helper method
        private ForumListingModel BuildForumListing(Post post)
        {
            var forum = post.Forum;
            return new ForumListingModel
            {
                ForumId = forum.ForumId,
                ForumTitle = forum.PostTitle,
                Description = forum.Description,
                AuthorId = forum.User.Id,
                AuthorName = forum.User.UserName ?? "Unknown", 
                AuthorRating = forum.User.Rating,
                AuthorUrl = post.User.ImagePath,
            };
        }
    }

    // Model for POST search request
    public class SearchRequestModel
    {
        public string? Query { get; set; }
    }
}