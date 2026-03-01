using Fan_Website.Infrastructure;
using Fan_Website.Models.ProfileComment;
using FanWebsiteAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileCommentController : ControllerBase
    {
        private readonly IApplicationUser userService;
        private readonly UserManager<ApplicationUser> userManager;

        public ProfileCommentController(IApplicationUser _userService, UserManager<ApplicationUser> _userManager)
        {
            userService = _userService;
            userManager = _userManager;
        }

        // GET: api/ProfileComment/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentTemplate(string id)
        {
            var userId = userManager.GetUserId(User);
            var currentUser = await userManager.FindByIdAsync(userId);
            if (currentUser == null)
                return Unauthorized();

            var user = userService.GetById(id);
            if (user == null)
                return NotFound();

            var model = new ProfileCommentDto
            {
                ProfileUserId = user.Id,
                ProfileUserName = user.UserName,
                ProfileUserImageUrl = user.ImagePath,
                ProfileUserRating = user.Rating,
                Date = DateTime.Now.ToString(),
                CommentUserId = currentUser.Id,
                CommentUserImagePath = currentUser.ImagePath,
                CommentUserName = currentUser.UserName,
                CommentUserRating = currentUser.Rating
            };
            return Ok(model);
        }

        // POST: api/ProfileComment
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] ProfileCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var comment = new ProfileComment
            {
                ProfileUser = userService.GetById(dto.ProfileUserId),
                Content = dto.CommentContent,
                CreateOn = DateTime.Now,
                CommentUser = currentUser
            };

            await userService.AddComment(comment);
            await userService.UpdateUserRating(currentUser.Id, typeof(ProfileComment));
            return CreatedAtAction(nameof(GetCommentTemplate), new { id = dto.ProfileUserId }, dto);
        }

        // PUT: api/ProfileComment/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> EditComment(int id, [FromBody] EditCommentDto dto)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var comment = await userService.GetCommentById(id);
            if (comment == null)
                return NotFound();

            if (comment.CommentUser.Id != currentUser.Id)
                return Forbid();

            comment.Content = dto.Content;
            await userService.UpdateComment(comment);
            return NoContent();
        }

        // DELETE: api/ProfileComment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var comment = await userService.GetCommentById(id);
            if (comment == null)
                return NotFound();

            if (comment.CommentUser.Id != currentUser.Id)
                return Forbid();

            await userService.DeleteComment(id);
            return NoContent();
        }
    }
}
