using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using Fan_Website.Models.Profile;
using Fan_Website.Models.ProfileComment;
using FanWebsiteAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

            var comments = BuildProfileComments(user.ProfileComments);
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
                Follows = user.Follows.Select(f => new FollowDto
                {
                    Id = f.Following?.Id ?? "",
                    UserName = f.Following?.UserName ?? "",
                    ImagePath = f.Following?.ImagePath,
                    Rating = f.Following?.Rating ?? 0,
                    MemberSince = f.Following?.MemberSince.ToString() ?? ""
                }),
                Followings = user.Followings.Select(f => new FollowDto
                {
                    Id = f.Follower?.Id ?? "",
                    UserName = f.Follower?.UserName ?? "",
                    ImagePath = f.Follower?.ImagePath,
                    Rating = f.Follower?.Rating ?? 0,
                    MemberSince = f.Follower?.ToString() ?? ""
                }),
                ProfileComments = comments,
                Bio = user.Bio,
                IsFollowing = user.Follows.Any(f => f.Follower.Id == currentUserId) 
            };

            return Ok(model);
        }

        // POST: api/Profile/UpdateFollows/{id}
        [HttpPost("UpdateFollows/{id}")]
        public IActionResult UpdateFollows(string id)
        {
            var user = userService.GetById(id);
            var currentUserId = userManager.GetUserId(User);
            var currentUser = userService.GetById(currentUserId);

            if (user == null || currentUser == null) return NotFound();

            var follow = new Follow
            {
                Following = user,
                Follower = currentUser
            };

            var existingFollow = user.Follows
                .FirstOrDefault(f => f.Follower == currentUser && f.Following == user);

            if (existingFollow != null)
            {
                context.Remove(existingFollow);
                user.Followers -= 1;
                currentUser.Following -= 1;
            }
            else
            {
                context.Add(follow);
                user.Followers += 1;
                currentUser.Following += 1;
                user.Follows.Add(follow);
                currentUser.Followings.Add(follow);
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
                    ImagePath = f.Follower?.ImagePath,
                    Rating = f.Follower?.Rating ?? 0,
                    MemberSince = f.Follower?.MemberSince.ToString()
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
                    ImagePath = f.Following?.ImagePath,
                    Rating = f.Following?.Rating ?? 0,
                    MemberSince = f.Following?.MemberSince.ToString()
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await userService.EditProfile(model.UserId, model.Bio, model.UserName);
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
            return comments.Select(c => new ProfileCommentDto
            {
                Id = c.Id,
                AuthorId = c.OtherUser.Id,
                AuthorName = c.OtherUser.UserName,
                AuthorImageUrl = c.OtherUser.ImagePath,
                AuthorRating = c.OtherUser.Rating,
                Date = c.CreateOn.ToString("yyyy-MM-dd HH:mm"),
                CommentContent = c.Content,
                UserId = c.CurrentUser.Id,
                OtherUserName = c.OtherUser?.UserName,
                OtherUserImagePath = c.OtherUser?.ImagePath,
                OtherUserRating = c.OtherUser?.Rating
            });
        }
    }
}