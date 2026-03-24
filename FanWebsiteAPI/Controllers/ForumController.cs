using Fan_Website.Infrastructure;
using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly IForum forumService;
        private readonly IPost postService;
        private readonly IApplicationUser userService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _context;

        public ForumController(IForum _forumService, IPost _postService, IApplicationUser _userService, UserManager<ApplicationUser> _userManager, AppDbContext context)
        {
            forumService = _forumService;
            postService = _postService;
            userService = _userService;
            userManager = _userManager;
            _context = context;
        }

        // GET: api/forum
        [HttpGet]
        public IActionResult GetAllForums()
        {
            var forums = forumService.GetAll()
                .Result.Select(forum => new ForumListingModel
                {
                    ForumId = forum.ForumId,
                    ForumTitle = forum.PostTitle,
                    Description = forum.Description,
                    AuthorId = forum.User.Id,
                    AuthorName = forum.User.UserName ?? "Unkown",
                    AuthorRating = forum.User.Rating
                });

            return Ok(forums);
        }

        // GET: api/forum/{id}?searchQuery=xyz
        [HttpGet("{id}")]
        public async Task<IActionResult> GetForumById(int id, [FromQuery] string? searchQuery, [FromQuery] int limit = 6)
        {
            var forum = await forumService.GetByIdAsync(id);

            if (forum == null)
                return NotFound();

            //var posts = postService.GetFilteredPosts(forum, searchQuery ?? "").ToList();

            var postListing = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.PostImages)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .OrderByDescending(p => p.CreatedOn)
                .Take(limit).Where(p => p.Forum == forum)
                .Select(post => new PostListingModel
                {
                    Id = post.PostId,
                    Title = post.Title,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorUrl = post.User.ImagePath, 
                    Content = post.Content, 
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.CreatedOn.ToString(),
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages
                })
                .ToListAsync();

            var result = new ForumTopicModel
            {
                Posts = postListing,
                Forum = BuildForumListing(forum)
            };

            return Ok(result);
        }

        // POST: api/forum
        [HttpPost]
        public async Task<IActionResult> AddForum([FromBody] AddForumModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = userManager.GetUserId(User);

            if (userId == null) throw new KeyNotFoundException("User Id not found."); 

            var user = await userManager.FindByIdAsync(userId);

            if (user == null) throw new NullReferenceException(nameof(user) + ": User was found as null.");

            var forum = new Forum
            {
                PostTitle = model.Title,
                Description = model.Description,
                CreatedOn = DateTime.Now,
                User = user
            };

            await forumService.Create(forum);
            await userService.UpdateUserRating(userId, typeof(Forum));

            return CreatedAtAction(nameof(GetForumById), new { id = forum.ForumId }, forum);
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

        // Helper Methods
        private ForumListingModel BuildForumListing(Post post)
        {
            return BuildForumListing(post.Forum);
        }

        private ForumListingModel BuildForumListing(Forum forum)
        {
            return new ForumListingModel
            {
                ForumId = forum.ForumId,
                ForumTitle = forum.PostTitle,
                Description = forum.Description,
                AuthorId = forum.User.Id,
                AuthorName = forum.User.UserName ?? "Unkown",
                AuthorRating = forum.User.Rating
            };
        }
    }
}