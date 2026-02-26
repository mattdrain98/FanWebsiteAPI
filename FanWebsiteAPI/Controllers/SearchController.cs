using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Models.Search;
using Fan_Website.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
                AuthorName = post.User.UserName,
                AuthorRating = post.User.Rating,
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
        // Optional: if front-end prefers POST requests
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
                AuthorName = post.User.UserName,
                AuthorRating = post.User.Rating,
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
                Id = forum.ForumId,
                Name = forum.PostTitle,
                Description = forum.Description
            };
        }
    }

    // Model for POST search request
    public class SearchRequestModel
    {
        public string Query { get; set; }
    }
}