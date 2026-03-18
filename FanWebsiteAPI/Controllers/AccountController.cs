using Fan_Website.DTOs;
using Fan_Website.Infrastructure;
using Fan_Website.ViewModel;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Service;
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
        [ApiExplorerSettings(IgnoreApi = true)]
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
        [ApiExplorerSettings(IgnoreApi = true)]
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

            var userEmail = user.Email ?? throw new MissingFieldException("Email is missing");

            await _emailSender.SendEmailAsync(userEmail, "Confirm your email",
                $@"<!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset='utf-8'>
                      <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    </head>
                    <body style='margin:0; padding:0; background-color:#f1f5f9; font-family: DM Sans, Segoe UI, sans-serif;'>

                      <table width='100%' cellpadding='0' cellspacing='0' style='padding: 40px 16px;'>
                        <tr>
                          <td align='center'>

                            <!-- Card -->
                            <table width='100%' cellpadding='0' cellspacing='0' 
                                   style='max-width:520px; background:#ffffff; border-radius:20px; 
                                          overflow:hidden; box-shadow: 0 4px 24px rgba(37,99,235,0.10);'>

                              <!-- Header -->
                              <tr>
                                <td align='center'
                                    style='background: linear-gradient(135deg, skyblue, #00AECC); 
                                           padding: 40px 32px 32px;'>
                                  <h1 style='margin:0; color:#ffffff; font-size:22px; 
                                             font-weight:700; letter-spacing:-0.5px;'>
                                    Dismino
                                  </h1>
                                  <p style='margin:8px 0 0; color:rgba(255,255,255,0.8); font-size:14px;'>
                                    Your adventure begins here
                                  </p>
                                </td>
                              </tr>

                              <!-- Body -->
                              <tr>
                                <td style='padding: 36px 32px 24px;'>
                                  <h2 style='margin:0 0 8px; color:#0f172a; font-size:20px; font-weight:700;'>
                                    Welcome, {user.UserName}!
                                  </h2>
                                  <p style='margin:0 0 24px; color:#64748b; font-size:15px; line-height:1.6;'>
                                    Thanks for joining the community. You're one step away — 
                                    just confirm your email address to activate your account.
                                  </p>

                                  <!-- Button -->
                                  <table width='100%' cellpadding='0' cellspacing='0'>
                                    <tr>
                                      <td align='center' style='padding: 8px 0 28px;'>
                                        <a href='{confirmUrl}'
                                           style='display:inline-block; padding:14px 36px;
                                                  background: linear-gradient(135deg, skyblue, #00AECC);
                                                  color:#ffffff; font-size:15px; font-weight:600;
                                                  text-decoration:none; border-radius:12px;
                                                  box-shadow: 0 4px 14px rgba(37,99,235,0.4);
                                                  letter-spacing:0.01em;'>
                                          Confirm Email →
                                        </a>
                                      </td>
                                    </tr>
                                  </table>

                                  <!-- Divider -->
                                  <hr style='border:none; border-top:1px solid #e2e8f0; margin: 0 0 24px;'/>

                                  <!-- Fallback link -->
                                  <p style='margin:0; color:#94a3b8; font-size:12px; line-height:1.6;'>
                                    Button not working? Copy and paste this link into your browser:
                                    <br/>
                                    <a href='{confirmUrl}' 
                                       style='color:#3b82f6; word-break:break-all; font-size:11px;'>
                                      {confirmUrl}
                                    </a>
                                  </p>
                                </td>
                              </tr>

                              <!-- Footer -->
                              <tr>
                                <td align='center'
                                    style='background:#f8fafc; border-top:1px solid #e2e8f0;
                                           padding: 20px 32px;'>
                                  <p style='margin:0; color:#94a3b8; font-size:12px; line-height:1.6;'>
                                    This link expires in 24 hours. If you didn't create an account, 
                                    you can safely ignore this email.
                                  </p>
                                  <p style='margin:8px 0 0; color:#cbd5e1; font-size:11px;'>
                                    © 2026 Dismino
                                  </p>
                                </td>
                              </tr>

                            </table>
                          </td>
                        </tr>
                      </table>

                    </body>
                    </html>");

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

        // POST: api/account/resend-confirmation
        [HttpPost("resend-confirmation")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> ResendConfirmation([FromBody] string email)
        {
            var user = await _userManager.FindByNameAsync(email);
            if (user == null) return Ok("If that email exists, a confirmation link has been sent.");

            if (await _userManager.IsEmailConfirmedAsync(user))
                return BadRequest("This email is already confirmed.");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var clientUrl = _configuration["App:ClientUrl"];
            var confirmUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            var userEmail = user.Email ?? throw new MissingFieldException("Email is missing");


            await _emailSender.SendEmailAsync(userEmail, "Confirm your email",
                $@"<!DOCTYPE html>
                   <html>
                   <head>
                     <meta charset='utf-8'>
                     <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                   </head>
                   <body style='margin:0; padding:0; background-color:#f1f5f9; font-family: DM Sans, Segoe UI, sans-serif;'>

                     <table width='100%' cellpadding='0' cellspacing='0' style='padding: 40px 16px;'>
                       <tr>
                         <td align='center'>

                           <!-- Card -->
                           <table width='100%' cellpadding='0' cellspacing='0' 
                                  style='max-width:520px; background:#ffffff; border-radius:20px; 
                                         overflow:hidden; box-shadow: 0 4px 24px rgba(37,99,235,0.10);'>

                             <!-- Header -->
                             <tr>
                               <td align='center'
                                   style='background: linear-gradient(135deg, skyblue, #00AECC); 
                                          padding: 40px 32px 32px;'>
                                 <h1 style='margin:0; color:#ffffff; font-size:22px; 
                                            font-weight:700; letter-spacing:-0.5px;'>
                                   Dismino
                                 </h1>
                                 <p style='margin:8px 0 0; color:rgba(255,255,255,0.8); font-size:14px;'>
                                   Your adventure begins here
                                 </p>
                               </td>
                             </tr>

                             <!-- Body -->
                             <tr>
                               <td style='padding: 36px 32px 24px;'>
                                 <h2 style='margin:0 0 8px; color:#0f172a; font-size:20px; font-weight:700;'>
                                   Welcome, {user.UserName}!
                                 </h2>
                                 <p style='margin:0 0 24px; color:#64748b; font-size:15px; line-height:1.6;'>
                                   Thanks for joining the community. You're one step away — 
                                   just confirm your email address to activate your account.
                                 </p>

                                 <!-- Button -->
                                 <table width='100%' cellpadding='0' cellspacing='0'>
                                   <tr>
                                     <td align='center' style='padding: 8px 0 28px;'>
                                       <a href='{confirmUrl}'
                                          style='display:inline-block; padding:14px 36px;
                                                 background: linear-gradient(135deg, skyblue, #00AECC);
                                                 color:#ffffff; font-size:15px; font-weight:600;
                                                 text-decoration:none; border-radius:12px;
                                                 box-shadow: 0 4px 14px rgba(37,99,235,0.4);
                                                 letter-spacing:0.01em;'>
                                         Confirm Email →
                                       </a>
                                     </td>
                                   </tr>
                                 </table>

                                 <!-- Divider -->
                                 <hr style='border:none; border-top:1px solid #e2e8f0; margin: 0 0 24px;'/>

                                 <!-- Fallback link -->
                                 <p style='margin:0; color:#94a3b8; font-size:12px; line-height:1.6;'>
                                   Button not working? Copy and paste this link into your browser:
                                   <br/>
                                   <a href='{confirmUrl}' 
                                      style='color:#3b82f6; word-break:break-all; font-size:11px;'>
                                     {confirmUrl}
                                   </a>
                                 </p>
                               </td>
                             </tr>

                             <!-- Footer -->
                             <tr>
                               <td align='center'
                                   style='background:#f8fafc; border-top:1px solid #e2e8f0;
                                          padding: 20px 32px;'>
                                 <p style='margin:0; color:#94a3b8; font-size:12px; line-height:1.6;'>
                                   This link expires in 24 hours. If you didn't create an account, 
                                   you can safely ignore this email.
                                 </p>
                                 <p style='margin:8px 0 0; color:#cbd5e1; font-size:11px;'>
                                   © 2026 Dismino
                                 </p>
                               </td>
                             </tr>

                           </table>
                         </td>
                       </tr>
                     </table>

                   </body>
                   </html>");

            return Ok("Confirmation email sent."); 
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

            var jwtTokenService = HttpContext.RequestServices.GetRequiredService<JwtTokenService>();
            var token = jwtTokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful",
                token = token,  
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