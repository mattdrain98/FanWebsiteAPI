using Fan_Website.Infrastructure;
using Fan_Website.Models.ProfileComment;
using FanWebsiteAPI.DTOs.ProfileComments;
using FanWebsiteAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileCommentController : ControllerBase
    {
        private readonly IApplicationUser _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ProfileCommentController(IApplicationUser userService, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _userService = userService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: api/ProfileComment/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentTemplate(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var currentUser = await _userManager.FindByIdAsync(user.Id);
            if (currentUser == null)
                return Unauthorized();

            if (user == null)
                return NotFound();

            var model = new ProfileCommentDto
            {
                ProfileUserId = user.Id,
                ProfileUserName = user.UserName,
                ProfileUserImageUrl = user.ImagePath,
                ProfileUserRating = user.Rating,
                DatePosted = DateTime.UtcNow.ToString("o"),
                AuthorId = currentUser.Id,
                AuthorImagePath = currentUser.ImagePath,
                AuthorName = currentUser.UserName,
                AuthorRating = currentUser.Rating
            };
            return Ok(model);
        }

        // POST: api/ProfileComment/add
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] AddProfileCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var profileUser = await _userService.GetById(dto.ProfileUserId);
            if (profileUser == null)
                return NotFound(new { message = "Profile not found" });

            if (currentUser.Id == dto.ProfileUserId)
                return BadRequest(new { message = "Cannot comment on your own profile" });

            try
            {
                await _notificationService.CreateAsync(
                    profileUser.Id,
                    $"{currentUser.UserName} commented on your profile",
                    "profile_comment",
                    $"/profile/{profileUser.Id}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification error: {ex.Message}");
            }

            var comment = new ProfileComment
            {
                ProfileUser = profileUser,
                Content = dto.CommentContent,
                UpdatedOn = DateTime.UtcNow,
                CommentUser = currentUser
            };

            await _userService.AddComment(comment);
            await _userService.UpdateUserRating(currentUser.Id, typeof(ProfileComment));

            return Ok(new { message = "Comment added successfully" });
        }

        // PUT: api/ProfileComment/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> EditComment(int id, [FromBody] EditProfileCommentDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var comment = await _userService.GetCommentById(id);
            if (comment == null)
                return NotFound();

            if (comment.CommentUser.Id != currentUser.Id)
                return Forbid();

            comment.Content = dto.CommentContent;
            await _userService.UpdateComment(comment);
            return NoContent();
        }

        // DELETE: api/ProfileComment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var comment = await _userService.GetCommentById(id);
            if (comment == null)
                return NotFound();

            if (comment.CommentUser.Id != currentUser.Id)
                return Forbid();

            await _userService.DeleteComment(id);
            return NoContent();
        }
    }
}
