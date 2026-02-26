using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using Fan_Website.Models.Profile;
using Fan_Website.Models.ProfileComment;
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

            var comments = BuildProfileComments(user.ProfileComments);

            var model = new ProfileModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                UserRating = user.Rating.ToString(),
                ProfileImageUrl = user.ImagePath,
                MemberSince = user.MemberSince,
                Following = user.Following,
                Followers = user.Followers,
                Follows = user.Follows,
                Followings = user.Followings,
                ProfileComments = comments,
                Bio = user.Bio
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

            // Make sure user.Follows is not null
            var follows = user.Follows ?? new List<Follow>();

            // Safely select follower names
            var followerNames = follows
                .Where(f => f.Follower != null)
                .Select(f => f.Follower.UserName)
                .ToList();

            return Ok(new
            {
                user.UserName,
                FollowersCount = user.Followers,
                Followers = followerNames
            });
        }

        // GET: api/Profile/Following/{id}
        [HttpGet("Following/{id}")]
        public IActionResult GetFollowing(string id)
        {
            var user = userService.GetById(id);
            if (user == null) return NotFound();

            var followings = user.Followings ?? new List<Follow>();

            var followingNames = followings
                .Where(f => f.Following != null)
                .Select(f => f.Following.UserName)
                .ToList();

            return Ok(new
            {
                user.UserName,
                FollowingCount = user.Following,
                Following = followingNames
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
        private IEnumerable<ProfileCommentModel> BuildProfileComments(IEnumerable<ProfileComment> comments)
        {
            return comments.Select(comment => new ProfileCommentModel
            {
                Id = comment.Id,
                AuthorImageUrl = comment.CurrentUser.ImagePath,
                AuthorName = comment.CurrentUser.UserName,
                AuthorId = comment.CurrentUser.Id,
                AuthorRating = comment.CurrentUser.Rating,
                Date = comment.CreateOn,
                CommentContent = comment.Content,
                OtherUserImagePath = comment.OtherUser.ImagePath,
                OtherUserName = comment.OtherUser.UserName,
                OtherUserRating = comment.OtherUser.Rating,
                UserId = comment.OtherUser.Id
            });
        }
    }
}