using Fan_Website.Services;
using FanWebsiteAPI.DTOs.Posts;
using FanWebsiteAPI.DTOs.Search;
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

        // GET: api/Search?query=keyword&page=1&pageSize=6
        [HttpGet]
        public async Task<IActionResult> Results(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query cannot be empty." });

            page = Math.Clamp(page, 1, 100);

            var allPosts = (await postService.GetFilteredPosts(query)).ToList();
            var totalPosts = allPosts.Count;
            var totalPages = Math.Min((int)Math.Ceiling(totalPosts / (double)pageSize), 100);

            var pagedPosts = allPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var postListings = pagedPosts.Select(post => new PostDto
            {
                PostId = post.PostId,
                Title = post.Title,
                AuthorId = post.User.Id,
                AuthorName = post.User.UserName ?? "Unknown",
                AuthorRating = post.User.Rating,
                AuthorImagePath = post.User.ImagePath,
                Content = post.Content,
                TotalLikes = post.TotalLikes,
                DatePosted = post.UpdatedOn.ToString(),
                RepliesCount = post.Replies?.Count ?? 0,
                ForumId = post.ForumId,
                ForumName = post.Forum?.PostTitle
            }).ToList();

            var result = new SearchResultDto
            {
                Posts = postListings,
                SearchQuery = query,
                EmptySearchResults = !postListings.Any(),
                Page = page,
                TotalPages = totalPages,
                TotalPosts = totalPosts
            };

            return Ok(result);
        }
    }

    public class SearchRequestModel
    {
        public string? Query { get; set; }
    }
}