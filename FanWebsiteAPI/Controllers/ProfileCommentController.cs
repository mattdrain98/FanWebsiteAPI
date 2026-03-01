using Fan_Website.Infrastructure;
using Fan_Website.Models.ProfileComment;
using FanWebsiteAPI.DTOs;
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
            var userId = userManager.GetUserId(User);
            var currentUser = await userManager.FindByIdAsync(userId);

            if (currentUser == null)
                return Unauthorized();

            var user = userService.GetById(id);

            if (user == null)
                return NotFound();

            var model = new ProfileCommentDto
            {
                AuthorId = currentUser.Id,
                AuthorName = currentUser.UserName,
                AuthorImageUrl = currentUser.ImagePath,
                AuthorRating = currentUser.Rating,
                Date = DateTime.Now.ToString(),
                UserId = user.Id,
                OtherUserImagePath = user.ImagePath,
                OtherUserName = user.UserName,
                OtherUserRating = user.Rating
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
                CurrentUser = currentUser,
                Content = dto.CommentContent,
                CreateOn = DateTime.Now,
                OtherUser = userService.GetById(dto.UserId)
            };

            await userService.AddComment(comment);
            await userService.UpdateUserRating(currentUser.Id, typeof(ProfileComment));

            return CreatedAtAction(nameof(GetCommentTemplate), new { id = dto.UserId }, dto);
        }
    }
}