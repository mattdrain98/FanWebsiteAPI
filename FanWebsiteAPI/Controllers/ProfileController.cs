using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using Fan_Website.Models.Profile;
using Fan_Website.Models.ProfileComment;
using FanWebsiteAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IApplicationUser userService;
        private readonly IUpload uploadService;
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
            uploadService = _uploadService;
            configuration = _configuration;
            context = ctx;
        }

        // GET: api/Profile/{id}
        [HttpGet("{id}")]
        public IActionResult GetProfile(string id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound();

            var comments = BuildProfileComments(user.ProfileComments ?? Enumerable.Empty<ProfileComment>());
            var currentUserId = userManager.GetUserId(User);

            var model = new ProfileDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                UserRating = user.Rating.ToString(),
                ProfileImageUrl = user.ImagePath,
                MemberSince = user.MemberSince.ToString(),
                Following = user.Following,
                Followers = user.Followers,
                Follows = (user.Follows ?? Enumerable.Empty<Follow>()).Select(f => new FollowDto
                {
                    Id = f.Following?.Id ?? "",
                    UserName = f.Following?.UserName ?? "",
                    ImagePath = f.Following?.ImagePath,
                    Rating = f.Following?.Rating ?? 0,
                    MemberSince = f.Following?.MemberSince.ToString() ?? ""
                }),
                Followings = (user.Followings ?? Enumerable.Empty<Follow>()).Select(f => new FollowDto
                {
                    Id = f.Follower?.Id ?? "",
                    UserName = f.Follower?.UserName ?? "",
                    ImagePath = f.Follower?.ImagePath,
                    Rating = f.Follower?.Rating ?? 0,
                    MemberSince = f.Follower?.MemberSince.ToString() ?? "" // ✅ fixed: was calling .ToString() on the Follow object itself
                }),
                ProfileComments = comments,
                Bio = user.Bio,
                IsFollowing = (user.Follows ?? Enumerable.Empty<Follow>())
                    .Any(f => f.Follower != null && f.Follower.Id == currentUserId) // ✅ null check added
            };

            return Ok(model);
        }

        // POST: api/Profile/UpdateFollows/{id}
        [HttpPost("UpdateFollows/{id}")]
        public IActionResult UpdateFollows(string id)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var user = userService.GetById(id);
            var currentUser = userService.GetById(currentUserId);

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
                var follow = new Follow
                {
                    Following = user,
                    Follower = currentUser
                };
                context.Add(follow);
                user.Followers += 1;
                currentUser.Following += 1;
            }

            context.Users.Update(user);
            context.Users.Update(currentUser);
            context.SaveChanges();

            return Ok(new { Followers = user.Followers, Following = currentUser.Following });
        }

        // GET: api/Profile/Followers/{id}
        [HttpGet("Followers/{id}")]
        public IActionResult GetFollowers(string id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound();

            var follows = user.Follows ?? new List<Follow>();

            var followersDto = follows
                .Where(f => f.Follower != null)
                .Select(f => new FollowDto
                {
                    Id = f.Follower.Id,
                    UserName = f.Follower.UserName,
                    ImagePath = f.Follower.ImagePath,
                    Rating = f.Follower.Rating,
                    MemberSince = f.Follower.MemberSince.ToString()
                })
                .ToList();

            return Ok(new
            {
                user.UserName,
                FollowersCount = followersDto.Count,
                Followers = followersDto
            });
        }

        // GET: api/Profile/Following/{id}
        [HttpGet("Following/{id}")]
        public IActionResult GetFollowing(string id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound();

            var followings = user.Followings ?? new List<Follow>();

            var followingDto = followings
                .Where(f => f.Following != null)
                .Select(f => new FollowDto
                {
                    Id = f.Following.Id,
                    UserName = f.Following.UserName,
                    ImagePath = f.Following.ImagePath,
                    Rating = f.Following.Rating,
                    MemberSince = f.Following.MemberSince.ToString()
                })
                .ToList();

            return Ok(new
            {
                user.UserName,
                FollowingCount = followingDto.Count,
                Following = followingDto
            });
        }

        // PUT: api/Profile/EditBio
        [HttpPut("EditBio")]
        public async Task<IActionResult> EditBio([FromBody] ProfileEditModel model)
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
        public async Task<IActionResult> EditUsername([FromBody] ProfileEditModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await userService.EditProfile(model.UserId, model.Bio, model.UserName);
            await signInManager.SignOutAsync();

            return Ok(new { Message = "Username updated successfully." });
        }

        // POST: api/Profile/UploadProfileImage
        [HttpPost("UploadProfileImage")]
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

        // Helper method
        private IEnumerable<ProfileCommentDto> BuildProfileComments(IEnumerable<ProfileComment> comments)
        {
            return comments
                .Where(c => c.ProfileUser != null && c.CommentUser != null) // ✅ skip any broken comments
                .Select(c => new ProfileCommentDto
                {
                    Id = c.Id,
                    ProfileUserId = c.ProfileUser.Id,
                    ProfileUserName = c.ProfileUser.UserName,
                    ProfileUserImageUrl = c.ProfileUser.ImagePath,
                    ProfileUserRating = c.ProfileUser.Rating,
                    Date = c.CreateOn.ToString("yyyy-MM-dd HH:mm"),
                    CommentContent = c.Content,
                    CommentUserId = c.CommentUser.Id,
                    CommentUserName = c.CommentUser.UserName ?? "",
                    CommentUserImagePath = c.CommentUser.ImagePath,
                    CommentUserRating = c.CommentUser.Rating
                });
        }
    }
}