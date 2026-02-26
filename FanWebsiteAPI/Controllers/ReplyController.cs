using Fan_Website.Infrastructure;
using Fan_Website.Models.Reply;
using Fan_Website.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReplyController : ControllerBase
    {
        private readonly IPost _postService;
        private readonly IApplicationUser _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReplyController(IPost postService, IApplicationUser userService, UserManager<ApplicationUser> userManager)
        {
            _postService = postService;
            _userService = userService;
            _userManager = userManager;
        }

        // GET: api/reply/create/5
        [HttpGet("create/{postId}")]
        public async Task<ActionResult<PostReplyDto>> GetReplyModel(int postId)
        {
            var post = _postService.GetById(postId);
            if (post == null) return NotFound("Post not found");

            var user = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (user == null) return Unauthorized("User not found");

            var dto = new PostReplyDto
            {
                PostId = post.PostId,
                PostTitle = post.Title,
                PostContent = post.Content,
                AuthorId = user.Id,
                AuthorName = user.UserName,
                AuthorImageUrl = user.ImagePath,
                AuthorRating = user.Rating,
                Date = DateTime.Now,
                ForumId = post.Forum.ForumId,
                ForumName = post.Forum.PostTitle
            };

            return Ok(dto);
        }

        // POST: api/reply/add
        [HttpPost("add")]
        public async Task<ActionResult> AddReply([FromBody] PostReplyDto model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized("User not found");

            var reply = BuildReply(model, user);

            await _postService.AddReply(reply);
            await _userService.UpdateUserRating(userId, typeof(PostReply));

            return Ok(new { Message = "Reply added successfully", PostId = model.PostId });
        }

        private PostReply BuildReply(PostReplyDto model, ApplicationUser user)
        {
            var post = _postService.GetById(model.PostId);

            return new PostReply
            {
                Post = post,
                Content = model.ReplyContent,
                CreateOn = DateTime.Now,
                User = user
            };
        }
    }

    // DTO for API use
    public class PostReplyDto
    {
        public int PostId { get; set; }
        public string PostTitle { get; set; } = null!;
        public string PostContent { get; set; } = null!;
        public string ReplyContent { get; set; } = null!; // For POST
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string AuthorImageUrl { get; set; } = null!;
        public int AuthorRating { get; set; }
        public DateTime Date { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; } = null!;
    }
}