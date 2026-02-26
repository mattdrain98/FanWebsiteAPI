using Fan_Website.Infrastructure;
using Fan_Website.Models.ProfileComment;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        // Retrieves a template for creating a comment (optional, can be used to prefill data in frontend)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentTemplate(string id)
        {
            var currentUser = await userManager.FindByNameAsync(User.Identity.Name);
            if (currentUser == null)
                return Unauthorized();

            var user = userService.GetById(id);
            if (user == null)
                return NotFound();

            var model = new ProfileCommentModel
            {
                AuthorId = currentUser.Id,
                AuthorName = currentUser.UserName,
                AuthorImageUrl = currentUser.ImagePath,
                AuthorRating = currentUser.Rating,
                Date = DateTime.Now,
                UserId = user.Id,
                OtherUserImagePath = user.ImagePath,
                OtherUserName = user.UserName,
                OtherUserRating = user.Rating
            };

            return Ok(model);
        }

        // POST: api/ProfileComment
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] ProfileCommentModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = userManager.GetUserId(User);
            var currentUser = await userManager.FindByIdAsync(userId);
            if (currentUser == null)
                return Unauthorized();

            var comment = BuildComment(model, currentUser);

            await userService.AddComment((ProfileComment)comment);
            await userService.UpdateUserRating(userId, typeof(ProfileComment));

            return CreatedAtAction(nameof(GetCommentTemplate), new { id = model.UserId }, comment);
        }

        // Helper method to build a ProfileComment entity from the model
        private ProfileComment BuildComment(ProfileCommentModel model, ApplicationUser currentUser)
        {
            var userProfile = userService.GetById(model.UserId);

            return new ProfileComment
            {
                CurrentUser = currentUser,
                Content = model.CommentContent,
                CreateOn = DateTime.Now,
                OtherUser = userProfile
            };
        }
    }
}