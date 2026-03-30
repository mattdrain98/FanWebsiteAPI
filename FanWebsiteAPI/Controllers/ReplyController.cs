using Fan_Website.Infrastructure;
using Fan_Website.Services;
using FanWebsiteAPI.DTOs;
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
            var post = await _postService.GetById(postId);
            if (post == null) return NotFound("Post not found");

            var userId = _userManager.GetUserId(User) ?? throw new MissingFieldException("Missing User Id");
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return Unauthorized("User not found");

            var dto = new PostReplyDto
            {
                PostId = post.PostId,
                PostTitle = post.Title,
                PostContent = post.Content,
                AuthorId = user.Id,
                AuthorName = user.UserName ?? "Unknown",
                AuthorImagePath = user.ImagePath,
                AuthorRating = user.Rating,
                DatePosted = DateTime.UtcNow.ToString(),
                ForumId = post.Forum.ForumId,
                ForumName = post.Forum.PostTitle
            };

            return Ok(dto);
        }

        // POST: api/reply/add
        [HttpPost("add")]
        public async Task<ActionResult> AddReply([FromBody] AddReplyDto model)
        {
            var userId = _userManager.GetUserId(User) ?? throw new KeyNotFoundException($"Missing User Id");
            var user = await _userManager.FindByIdAsync(userId); 
            if (user == null) return Unauthorized("User not found");

            var post = await _postService.GetById(model.PostId);
            if (post == null) return NotFound("Post not found");

            var reply = new PostReply
            {
                Post = post,
                ReplyContent = model.ReplyContent ?? "Unknown",
                UpdatedOn = DateTime.Now,
                User = user
            };

            await _postService.AddReply(reply);
            await _userService.UpdateUserRating(userId, typeof(PostReply));
            return Ok(new { Message = "Reply added successfully", PostId = model.PostId });
        }

        // PUT: api/reply/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> EditReply(int id, [FromBody] EditReplyDto model)
        {
            var userId = _userManager.GetUserId(User);
            var reply = await _postService.GetReplyByIdAsync(id);
            if (reply == null) return NotFound("Reply not found");
            if (reply.User.Id != userId) return Forbid();
            await _postService.EditReply(id, model.Content);
            return NoContent();
        }

        // DELETE: api/reply/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteReply(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reply = await _postService.GetReplyByIdAsync(id);
            if (reply == null) return NotFound("Reply not found");
            if (reply.User.Id != userId) return Forbid();
            await _postService.DeleteReply(id);
            return NoContent();
        }
    }
}