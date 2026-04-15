using Fan_Website.Infrastructure;
using Fan_Website.Services;
using FanWebsiteAPI.DTOs.Forums;
using FanWebsiteAPI.DTOs.Posts;
using FanWebsiteAPI.DTOs.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly IForum forumService;
        private readonly IApplicationUser userService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _context;

        public ForumController(IForum _forumService, IPost _postService, IApplicationUser _userService, UserManager<ApplicationUser> _userManager, AppDbContext context)
        {
            forumService = _forumService;
            userService = _userService;
            userManager = _userManager;
            _context = context;
        }

        // GET: api/forum
        [HttpGet]
        public async Task<IActionResult> GetAllForums()
        {
            var forums = await forumService.GetAll();

            var result = forums
                .Select(forum => new ForumDto
                {
                    ForumId = forum.ForumId,
                    ForumTitle = forum.PostTitle,
                    Description = forum.Description,
                    AuthorId = forum.User.Id,
                    AuthorName = forum.User.UserName ?? "Unknown",
                    AuthorRating = forum.User.Rating,
                    AuthorImagePath = forum.User.ImagePath,
                    DatePosted = forum.UpdatedOn.ToString("o"),
                    PostsCount = forum.Posts.Count()
                });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetForumById(int id, [FromQuery] string? searchQuery, [FromQuery] int page = 1, [FromQuery] int limit = 6)
        {
            var forum = await forumService.GetByIdAsync(id);
            if (forum == null)
                return NotFound();

            page = Math.Clamp(page, 1, 100);

            var query = _context.Posts
                .Where(p => p.ForumId == forum.ForumId &&
                            (string.IsNullOrEmpty(searchQuery) || p.Title.Contains(searchQuery)));

            var totalPosts = await query.CountAsync();
            var totalPages = Math.Min((int)Math.Ceiling(totalPosts / (double)limit), 100);

            var postListing = await query
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.PostImages)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .OrderByDescending(p => p.UpdatedOn)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(post => new PostDto
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
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();

            var result = new SearchResultDto
            {
                Posts = postListing,
                Forum = BuildForumListing(forum),
                Page = page,
                TotalPages = totalPages,
                TotalPosts = totalPosts
            };

            return Ok(result);
        }

        // POST: api/forum
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddForum([FromBody] AddForumDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User not found" });

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            var forum = new Forum
            {
                PostTitle = model.Title,
                Description = model.Description,
                UpdatedOn = DateTime.UtcNow, 
                User = user
            };

            await forumService.Create(forum);
            await userService.UpdateUserRating(userId, typeof(Forum));

            return CreatedAtAction(nameof(GetForumById), new { id = forum.ForumId }, new
            {
                forumId = forum.ForumId,
                forumTitle = forum.PostTitle,
                description = forum.Description
            });
        }

        // DELETE: api/forum/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForum(int id)
        {
            var forum = await forumService.GetByIdAsync(id);
            if (forum == null)
                return NotFound();

            await forumService.Delete(id);
            return NoContent();
        }

        private ForumDto BuildForumListing(Forum forum)
        {
            return new ForumDto
            {
                ForumId = forum.ForumId,
                ForumTitle = forum.PostTitle,
                Description = forum.Description,
                AuthorId = forum.User.Id,
                AuthorName = forum.User.UserName ?? "Unkown",
                AuthorRating = forum.User.Rating,
                AuthorImagePath = forum.User.ImagePath,
                DatePosted = forum.UpdatedOn.ToString()
            };
        }
    }
}