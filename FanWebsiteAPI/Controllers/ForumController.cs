using Fan_Website.Infrastructure;
using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        public ForumController(IForum _forumService, IPost _postService, IApplicationUser _userService, UserManager<ApplicationUser> _userManager)
        {
            forumService = _forumService;
            postService = _postService;
            userService = _userService;
            userManager = _userManager;
        }

        // GET: api/forum
        [HttpGet]
        public IActionResult GetAllForums()
        {
            var forums = forumService.GetAll()
                .Select(forum => new ForumListingModel
                {
                    Id = forum.ForumId,
                    Name = forum.PostTitle,
                    Description = forum.Description,
                    AuthorId = forum.User.Id,
                    AuthorName = forum.User.UserName,
                    AuthorRating = forum.User.Rating.ToString()
                });

            return Ok(forums);
        }

        // GET: api/forum/{id}?searchQuery=xyz
        [HttpGet("{id}")]
        public IActionResult GetForumById(int id, [FromQuery] string? searchQuery)
        {
            var forum = forumService.GetById(id);
            if (forum == null)
                return NotFound();

            var posts = postService.GetFilteredPosts(forum, searchQuery).ToList();

            var postListings = posts.Select(post => new PostListingModel
            {
                Id = post.PostId,
                AuthorId = post.User.Id,
                AuthorRating = post.User.Rating,
                AuthorName = post.User.UserName,
                Title = post.Title,
                TotalLikes = post.TotalLikes,
                DatePosted = post.CreatedOn.ToString(),
                RepliesCount = post.Replies.Count(),
                Forum = BuildForumListing(post)
            });

            var result = new ForumTopicModel
            {
                Posts = postListings,
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
            var user = await userManager.FindByIdAsync(userId);

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
            var forum = forumService.GetById(id);
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
                Id = forum.ForumId,
                Name = forum.PostTitle,
                Description = forum.Description,
                AuthorId = forum.User.Id,
                AuthorName = forum.User.UserName,
                AuthorRating = forum.User.Rating.ToString()
            };
        }
    }
}