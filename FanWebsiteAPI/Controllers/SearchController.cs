using Fan_Website.Infrastructure;
using Fan_Website.Services;
using FanWebsiteAPI.Controllers;
using FanWebsiteAPI.DTOs.Posts;
using FanWebsiteAPI.DTOs.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : BaseApiController
    {
        private const int MaxRankedCandidates = 1000;

        private readonly IPost _postService;
        private readonly IPostSearchIndex _searchIndex;

        public SearchController(IPost postService, IPostSearchIndex searchIndex)
        {
            _postService = postService;
            _searchIndex = searchIndex;
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

            page = ClampPage(page);
            var canModerate = CanModerate;

            var candidateIds = _searchIndex.Search(query).Take(MaxRankedCandidates).ToList();

            if (candidateIds.Count == 0)
            {
                return Ok(new SearchResultDto
                {
                    Posts = new List<PostDto>(),
                    SearchQuery = query,
                    EmptySearchResults = true,
                    Page = page,
                    TotalPages = 0,
                    TotalPosts = 0
                });
            }

            List<int> visibleIds;
            if (canModerate)
            {
                visibleIds = candidateIds;
            }
            else
            {
                var hiddenIds = await _postService.Query()
                    .Where(p => candidateIds.Contains(p.PostId) && p.User.IsHidden)
                    .Select(p => p.PostId)
                    .ToHashSetAsync();

                visibleIds = candidateIds.Where(id => !hiddenIds.Contains(id)).ToList();
            }

            var totalPosts = visibleIds.Count;
            var totalPages = Math.Min((int)Math.Ceiling(totalPosts / (double)pageSize), 100);

            var pageIds = visibleIds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagePosts = await _postService.Query()
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.Replies)
                .Where(p => pageIds.Contains(p.PostId))
                .ToListAsync();

            var postsById = pagePosts.ToDictionary(p => p.PostId);
            var orderedPosts = pageIds
                .Select(id => postsById.TryGetValue(id, out var p) ? p : null)
                .Where(p => p != null)
                .Select(p => p!);

            var postListings = orderedPosts.Select(post => new PostDto
            {
                PostId = post.PostId,
                Title = post.Title,
                AuthorId = post.User.Id,
                AuthorName = post.User.UserName ?? "Unknown",
                AuthorRating = post.User.Rating,
                AuthorImagePath = post.User.IsHidden ? null : post.User.ImagePath,
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
