using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fan_Website.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IUpload _uploadService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImagesController> _logger;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly Dictionary<string, string> _contentTypes = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        public ImagesController(
            IUpload uploadService,
            IConfiguration configuration,
            ILogger<ImagesController> logger)
        {
            _uploadService = uploadService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file provided" });

                if (file.Length > _maxFileSize)
                    return BadRequest(new { message = "File is too large. Maximum size is 5MB." });

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!Array.Exists(_allowedExtensions, ext => ext == extension))
                    return BadRequest(new { message = "Invalid file type. Only JPG, PNG, GIF, and WebP are allowed." });

                var connectionString = _configuration.GetConnectionString("AzureStorageAccount");
                var container = _uploadService.GetBlobContainer(connectionString, "post-images");

                var blobName = $"{Guid.NewGuid()}{extension}";
                var blobClient = container.GetBlobClient(blobName);

                var headers = new BlobHttpHeaders
                {
                    ContentType = _contentTypes.GetValueOrDefault(extension, "application/octet-stream")
                };

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = headers
                });

                var url = blobClient.Uri.ToString();
                _logger.LogInformation($"File uploaded to blob: {url}");

                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteImage([FromBody] DeleteImageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Url))
                    return BadRequest(new { message = "No URL provided" });

                var uri = new Uri(request.Url);
                var blobName = Path.GetFileName(uri.LocalPath);

                var connectionString = _configuration.GetConnectionString("AzureStorageAccount");
                var container = _uploadService.GetBlobContainer(connectionString, "post-images"); 

                var blobClient = container.GetBlobClient(blobName);

                var exists = await blobClient.ExistsAsync();
                if (!exists)
                    return NotFound(new { message = "File not found" });

                await blobClient.DeleteAsync();
                _logger.LogInformation($"Blob deleted: {blobName}");

                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
            }
        }
    }

    public class DeleteImageRequest
    {
        public required string Url { get; set; }
    }
}