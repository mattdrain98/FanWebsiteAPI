using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using FanWebsiteAPI.DTOs.Follow;
using FanWebsiteAPI.DTOs.Profile;
using FanWebsiteAPI.DTOs.ProfileComments;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IApplicationUser userService;
        private readonly IConfiguration configuration;
        private readonly AppDbContext context;

        public ProfileController(UserManager<ApplicationUser> _userManager,
                                 SignInManager<ApplicationUser> _signInManager,
                                 IApplicationUser _userService,
                                 IUpload _uploadService,
                                 IConfiguration _configuration,
                                 AppDbContext ctx)
        {
            userManager = _userManager;
            signInManager = _signInManager;
            userService = _userService;
            configuration = _configuration;
            context = ctx;
        }

        // GET: api/Profile/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(string id)
        {
            var currentUserId = userManager.GetUserId(User);

            var user = await context.Users
                .Include(u => u.Follows)
                    .ThenInclude(f => f.Follower)
                .Include(u => u.Followings)
                    .ThenInclude(f => f.Following)
                .Include(u => u.ProfileComments)
                    .ThenInclude(c => c.CommentUser)
                .Include(u => u.ProfileComments)
                    .ThenInclude(c => c.ProfileUser)
                .Where(u => u.Id == id)
                .Select(u => new ProfileDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    UserRating = u.Rating, 
                    ProfileImageUrl = u.ImagePath,
                    MemberSince = u.MemberSince.ToString("o"), 
                    Following = u.Following,
                    Followers = u.Followers,
                    Bio = u.Bio,
                    IsFollowing = context.Set<Follow>()
                        .Any(f => f.Follower.Id == currentUserId && f.Following.Id == u.Id),
                    Follows = u.Follows.Select(f => new FollowDto
                    {
                        Id = f.Following.Id,
                        UserName = f.Following.UserName ?? "",
                        ImagePath = f.Following.ImagePath,
                        Rating = f.Following.Rating,
                        MemberSince = f.Following.MemberSince.ToString("o") 
                    }).ToList(),
                    Followings = u.Followings.Select(f => new FollowDto
                    {
                        Id = f.Follower.Id,
                        UserName = f.Follower.UserName ?? "",
                        ImagePath = f.Follower.ImagePath,
                        Rating = f.Follower.Rating,
                        MemberSince = f.Follower.MemberSince.ToString("o") 
                    }).ToList(),
                    ProfileComments = u.ProfileComments.Select(c => new ProfileCommentDto
                    {
                        Id = c.Id,
                        CommentContent = c.Content,
                        AuthorId = c.CommentUser.Id,
                        AuthorName = c.CommentUser.UserName ?? "",
                        AuthorImagePath = c.CommentUser.ImagePath,
                        AuthorRating = c.CommentUser.Rating,
                        ProfileUserId = c.ProfileUser.Id,
                        ProfileUserImageUrl = c.ProfileUser.ImagePath,
                        ProfileUserName = c.ProfileUser.UserName ?? "",
                        ProfileUserRating = c.ProfileUser.Rating,
                        DatePosted = c.UpdatedOn.ToString("o") 
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/Profile/UpdateFollows/{id}
        [HttpPost("UpdateFollows/{id}")]
        public async Task<IActionResult> UpdateFollows(string id)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var user = await userService.GetById(id);
            var currentUser = await userService.GetById(currentUserId);

            if (user == null || currentUser == null) return NotFound();

            var existingFollow = context.Set<Follow>()
                .FirstOrDefault(f => f.Follower.Id == currentUserId && f.Following.Id == id);

            if (existingFollow != null)
            {
                context.Remove(existingFollow);
                user.Followers = Math.Max(0, user.Followers - 1);
                currentUser.Following = Math.Max(0, currentUser.Following - 1);
            }
            else
            {
                context.Add(new Follow
                {
                    Following = user,
                    Follower = currentUser
                });
                user.Followers += 1;
                currentUser.Following += 1;
            }

            context.Users.Update(user);
            context.Users.Update(currentUser);
            await context.SaveChangesAsync();

            return Ok(new { Followers = user.Followers, Following = currentUser.Following });
        }

        // GET: api/Profile/Followers/{id}
        [HttpGet("Followers/{id}")]
        public async Task<IActionResult> GetFollowers(string id)
        {
            var followerIds = await context.Set<Follow>()
                .Where(f => f.Following.Id == id)
                .Select(f => f.Follower.Id)
                .ToListAsync();

            var followers = await context.Users
                .Where(u => followerIds.Contains(u.Id))
                .Select(u => new FollowDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    ImagePath = u.ImagePath,
                    Rating = u.Rating,
                    MemberSince = u.MemberSince.ToString("o")
                })
                .ToListAsync();

            var userName = await context.Users
                .Where(u => u.Id == id)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                UserName = userName,
                FollowersCount = followers.Count,
                Followers = followers
            });
        }

        // GET: api/Profile/Following/{id}
        [HttpGet("Following/{id}")]
        public async Task<IActionResult> GetFollowing(string id)
        {
            var followingIds = await context.Set<Follow>()
                .Where(f => f.Follower.Id == id)
                .Select(f => f.Following.Id)
                .ToListAsync();

            var following = await context.Users
                .Where(u => followingIds.Contains(u.Id))
                .Select(u => new FollowDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    ImagePath = u.ImagePath,
                    Rating = u.Rating,
                    MemberSince = u.MemberSince.ToString("o")
                })
                .ToListAsync();

            var userName = await context.Users
                .Where(u => u.Id == id)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                UserName = userName,
                FollowingCount = following.Count,
                Following = following
            });
        }

        // PUT: api/Profile/EditBio
        [HttpPut("EditBio")]
        public async Task<IActionResult> EditBio([FromBody] EditProfileDto model)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId != model.UserId) return Forbid();
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();
            user.Bio = model.Bio;
            await userManager.UpdateAsync(user);
            return Ok(new { Message = "Bio updated successfully." });
        }

        // PUT: api/Profile/EditUsername
        [HttpPut("EditUsername")]
        public async Task<IActionResult> EditUsername([FromBody] EditProfileDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await userService.EditProfile(model.UserId, model.Bio, model.UserName);
            await signInManager.SignOutAsync();

            return Ok(new { Message = "Username updated successfully." });
        }

        // POST: api/Profile/UploadProfileImage
        [HttpPost("UploadProfileImage")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please provide a valid image." });

            var userId = userManager.GetUserId(User);
            var connectionString = configuration.GetConnectionString("AzureStorageAccount");

            var containerClient = new BlobContainerClient(connectionString, "profile-images");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var fileName = $"{userId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(fileName);

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            await userService.SetProfileImage(userId, blobClient.Uri);

            return Ok(new { ImageUrl = blobClient.Uri.ToString() });
        }
    }
}