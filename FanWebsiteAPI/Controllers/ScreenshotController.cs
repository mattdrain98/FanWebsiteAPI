using Fan_Website.Infrastructure;
using FanWebsiteAPI.DTOs.Screenshots;
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
        public async Task<ActionResult<IEnumerable<ScreenshotDto>>> GetAllScreenshots()
        {
            var screenshots = await _screenshotService.GetAll(); 

            var result = screenshots
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName,
                    AuthorRating = s.User.Rating,
                    AuthorImagePath = s.User.ImagePath,
                    DatePosted = s.UpdatedOn.ToString("o"),
                    ImageUrl = s.ImagePath,
                    Slug = s.ScreenshotTitle?.Replace(' ', '-').ToLower() ?? ""
                }).ToList();

            return Ok(result);
        }

        // GET: api/screenshot/user
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<ScreenshotDto>>> GetUserScreenshots()
        {
            var userId = _userManager.GetUserId(User);
            var screenshots = await _screenshotService.GetAll(); 

            var result = screenshots
                .Where(s => s.User.Id == userId)
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName,
                    AuthorRating = s.User.Rating,
                    DatePosted = s.UpdatedOn.ToString("o"),
                    ImageUrl = s.ImagePath,
                    Slug = s.ScreenshotTitle?.Replace(' ', '-').ToLower() ?? ""
                }).ToList();

            return Ok(result);
        }

        // POST: api/screenshot
        [HttpPost]
        public async Task<ActionResult> AddScreenshot([FromForm] AddScreenshotDto model)
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
                UpdatedOn = DateTime.UtcNow,
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
}