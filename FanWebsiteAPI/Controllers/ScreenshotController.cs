using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs; 
using Fan_Website.Infrastructure;
using Fan_Website.Models.Screenshot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Controllers
{
    public class ScreenshotController: Controller
    {
        private AppDbContext context { get; set; }

        private readonly IApplicationUser userService;
        private readonly IConfiguration configuration;
        private readonly IUpload uploadService;
        private readonly IScreenshot screenshotService; 
        private readonly UserManager<ApplicationUser> userManager; 
        public ScreenshotController(AppDbContext ctx, IApplicationUser _userService, 
            IConfiguration _configuration, IUpload _uploadService, IScreenshot _screenshotService, UserManager<ApplicationUser> _userManager)
        {
            context = ctx;
            userService = _userService;
            configuration = _configuration;
            uploadService = _uploadService;
            screenshotService = _screenshotService;
            userManager = _userManager; 
        }
        public IActionResult Index()
        {
            var screenshots = screenshotService.GetAll()
                .Select(screenshot => new ScreenshotListingModel
                {
                    Id = screenshot.ScreenshotId,
                    Content = screenshot.ScreenshotDescription,
                    Title = screenshot.ScreenshotTitle,
                    AuthorId = screenshot.User.Id,
                    AuthorName = screenshot.User.UserName,
                    AuthorRating = screenshot.User.Rating,
                    DatePosted = screenshot.CreatedOn.ToString(), 
                    ImageUrl = screenshot.ImagePath

                });

            var model = new ScreenshotIndexModel
            {
                ScreenshotList = screenshots
            };
            return View(model);
        }
        public IActionResult UserScreenshots()
        {
            var screenshots = screenshotService.GetAll()
                .Select(screenshot => new ScreenshotListingModel
            {
                Id = screenshot.ScreenshotId,
                Content = screenshot.ScreenshotDescription,
                Title = screenshot.ScreenshotTitle,
                AuthorId = screenshot.User.Id,
                AuthorName = screenshot.User.UserName,
                AuthorRating = screenshot.User.Rating,
                DatePosted = screenshot.CreatedOn.ToString(),
                ImageUrl = screenshot.ImagePath

            }); ;

            var model = new ScreenshotIndexModel
            {
                ScreenshotList = screenshots 
            };

            return View(model); 
        }

        public IActionResult Create()
        {
            var model = new AddScreenshotModel();
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> AddScreenshot(AddScreenshotModel model)
        {
            var imageUri = model.ImageFile.FileName;

            imageUri = await UploadScreenshotImageAsync(model.ImageFile);

            var userId = userManager.GetUserId(User);
            var user = await userManager.FindByIdAsync(userId);
            var screenshot = new Screenshot
            {
                ScreenshotTitle = model.Title,
                ScreenshotDescription = model.Content,
                CreatedOn = DateTime.Now,
                ImagePath = imageUri,
                User = user 
            }; 
            await screenshotService.Add(screenshot); 
            await userService.UpdateUserRating(userId, typeof(Screenshot));
            return RedirectToAction("Index", "Screenshot"); 
        }

        private Screenshot BuildScreenshot(NewScreenshotModel model, ApplicationUser user)
        {
            return new Screenshot
            {
                ScreenshotId = model.Id, 
                ScreenshotTitle = model.Title, 
                ScreenshotDescription = model.Content, 
                ImagePath = model.ScreenshotImageUrl, 
                CreatedOn = DateTime.Now, 
                User = user
            }; 
        }

        private async Task<string> UploadScreenshotImageAsync(IFormFile file)
        {
            var connectionString =
                configuration.GetConnectionString("AzureStorageAccount");

            var container = uploadService.GetBlobContainer(
                connectionString,
                "screenshot-images");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = container.GetBlobClient(fileName);

            await blobClient.UploadAsync(
                file.OpenReadStream(),
                overwrite: true);

            return blobClient.Uri.AbsoluteUri;
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var screenshot = context.Screenshots.Find(id);
            return View(screenshot);
        }

        [HttpPost]
        public IActionResult Delete(Screenshot screenshot)
        {
            context.Screenshots.Remove(screenshot);
            context.SaveChanges();
            return RedirectToAction("Index", "Screenshot");
        }
    }
}
