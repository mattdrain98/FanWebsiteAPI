using Fan_Website.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScreenshotController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IApplicationUser _userService;
        private readonly IConfiguration _configuration;
        private readonly IUpload _uploadService;
        private readonly IScreenshot _screenshotService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScreenshotController(
            AppDbContext context,
            IApplicationUser userService,
            IConfiguration configuration,
            IUpload uploadService,
            IScreenshot screenshotService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userService = userService;
            _configuration = configuration;
            _uploadService = uploadService;
            _screenshotService = screenshotService;
            _userManager = userManager;
        }

        // GET: api/screenshot
        [HttpGet]
        public ActionResult<IEnumerable<ScreenshotDto>> GetAllScreenshots()
        {
            var screenshots = _screenshotService.GetAll()
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName,
                    AuthorRating = s.User.Rating,
                    DatePosted = s.CreatedOn,
                    ImageUrl = s.ImagePath,
                    Slug = s.ScreenshotTitle?.Replace(' ', '-').ToLower() ?? ""
                }).ToList();

            return Ok(screenshots);
        }

        // GET: api/screenshot/user
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<ScreenshotDto>>> GetUserScreenshots()
        {
            var userId = _userManager.GetUserId(User);
            var screenshots = _screenshotService.GetAll()
                .Where(s => s.User.Id == userId)
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName,
                    AuthorRating = s.User.Rating,
                    DatePosted = s.CreatedOn,
                    ImageUrl = s.ImagePath,
                    Slug = s.ScreenshotTitle?.Replace(' ', '-').ToLower() ?? ""
                }).ToList();

            return Ok(screenshots);
        }

        // POST: api/screenshot
        [HttpPost]
        public async Task<ActionResult> AddScreenshot([FromForm] NewScreenshotDto model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized("User not found");

            var imageUri = model.ImageFile != null
                ? await UploadScreenshotImageAsync(model.ImageFile)
                : null;

            var screenshot = new Screenshot
            {
                ScreenshotTitle = model.Title,
                ScreenshotDescription = model.Content,
                CreatedOn = DateTime.Now,
                ImagePath = imageUri ?? "",
                User = user
            };

            await _screenshotService.Add(screenshot);
            await _userService.UpdateUserRating(userId, typeof(Screenshot));

            return Ok(new { Message = "Screenshot added successfully", ScreenshotId = screenshot.ScreenshotId });
        }

        private async Task<string> UploadScreenshotImageAsync(IFormFile file)
        {
            var connectionString = _configuration.GetConnectionString("AzureStorageAccount");
            var container = _uploadService.GetBlobContainer(connectionString, "screenshot-images");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = container.GetBlobClient(fileName);

            await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);

            return blobClient.Uri.AbsoluteUri;
        }

        // DELETE: api/screenshot/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteScreenshot(int id)
        {
            var screenshot = _context.Screenshots.Find(id);
            if (screenshot == null) return NotFound("Screenshot not found");

            _context.Screenshots.Remove(screenshot);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Screenshot deleted successfully" });
        }
    }

    // DTOs for API
    public class ScreenshotDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public int AuthorRating { get; set; }
        public DateTime DatePosted { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }

    public class NewScreenshotDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public IFormFile? ImageFile { get; set; }
    }
}