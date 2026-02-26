using Fan_Website.DTOs;
using Fan_Website.Infrastructure;
using Fan_Website.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IApplicationUser _userService;

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IUnitOfWork unitOfWork, IApplicationUser userService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        // GET: api/account/users
        [HttpGet("users")]
        public ActionResult<IEnumerable<ApplicationUser>> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }

        // GET: api/account/new-users
        [HttpGet("new-users")]
        public ActionResult<IEnumerable<ApplicationUser>> GetLatestUsers()
        {
            var latestUsers = _userService.GetLatestUsers(10);
            return Ok(latestUsers);
        }

        // POST: api/account/edit-profile
        [HttpPost("edit-profile")]
        public async Task<ActionResult> EditProfile([FromForm] IFormFile file, [FromForm] EditProfileDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found");

            if (file != null)
            {
                await _unitOfWork.UploadImageAsync(file);
                user.ImagePath = file.FileName;
            }

            user.UserName = model.UserName ?? user.UserName;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return Ok(new { Message = "Profile updated successfully" });
        }

        // POST: api/account/change-password
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized("User not found");

            var result = await _userManager.ChangePasswordAsync(user, model.Password, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            await _signInManager.RefreshSignInAsync(user);
            return Ok(new { Message = "Password changed successfully" });
        }

        // POST: api/account/logout
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully" });
        }

        // POST: api/account/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromForm] RegisterDto model, [FromForm] IFormFile file)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                MemberSince = DateTime.Now,
                Followers = 0,
                Following = 0,
                ImagePath = file?.FileName
            };

            if (file != null) await _unitOfWork.UploadImageAsync(file);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return Ok(new { Message = "Registration successful", UserId = user.Id });
        }

        // POST: api/account/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, false);

            if (!result.Succeeded) return Unauthorized("Invalid login attempt");

            return Ok(new { Message = "Login successful" });
        }
    }
}