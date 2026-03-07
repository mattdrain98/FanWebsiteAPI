using Fan_Website.DTOs;
using Fan_Website.Infrastructure;
using Fan_Website.ViewModel;
using FanWebsiteAPI.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

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
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration; 
        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IUnitOfWork unitOfWork, IApplicationUser userService, IEmailSender emailSender, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _emailSender = emailSender;
            _configuration = configuration; 
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
                try
                {
                    await _unitOfWork.UploadImageAsync(file);
                    user.ImagePath = file.FileName;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"File upload failed: {ex.Message}");
                }
            }

            user.UserName = model.UserName ?? user.UserName;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

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
        public async Task<ActionResult> Register([FromForm] RegisterDto model, [FromForm] IFormFile? file)
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
            
            // Generate token and send email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var clientUrl = _configuration["App:ClientUrl"];
            var confirmUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $@"<h2>Welcome to Final Fantasy Fan Site!</h2>
           <p>Please confirm your email by clicking the link below:</p>
           <a href='{confirmUrl}' 
              style='padding:10px 20px;background:#2563eb;color:white;
                     border-radius:8px;text-decoration:none;'>
             Confirm Email
           </a>");

            return Ok("Registration successful. Please check your email.");
        }

        // GET: api/account/confirm-email
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return BadRequest("Invalid or expired confirmation link.");

            return Ok("Email confirmed. You can now log in.");
        }

        // POST: api/account/login 
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null) return Unauthorized("Invalid login attempt");

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized("Please confirm your email before logging in.");

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, false);
            if (!result.Succeeded) return Unauthorized("Invalid login attempt");

            return Ok(new
            {
                message = "Login successful",
                user = new
                {
                    userId = user.Id,
                    userName = user.UserName,
                    imagePath = user.ImagePath
                }
            });
        }
    }
}