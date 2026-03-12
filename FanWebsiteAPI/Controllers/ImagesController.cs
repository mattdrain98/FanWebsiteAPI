using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImagesController> _logger;
        private readonly string _uploadFolder = "uploads/posts";
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ImagesController(IWebHostEnvironment env, ILogger<ImagesController> logger)
        {
            _env = env;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Upload request received");

                // Validate file
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file provided");
                    return BadRequest(new { message = "No file provided" });
                }

                _logger.LogInformation($"File: {file.FileName}, Size: {file.Length}");

                // Validate file size
                if (file.Length > _maxFileSize)
                {
                    _logger.LogWarning($"File too large: {file.Length} bytes");
                    return BadRequest(new { message = "File is too large. Maximum size is 5MB." });
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!Array.Exists(_allowedExtensions, ext => ext == extension))
                {
                    _logger.LogWarning($"Invalid file extension: {extension}");
                    return BadRequest(new { message = "Invalid file type. Only JPG, PNG, GIF, and WebP are allowed." });
                }

                // Create upload folder if it doesn't exist
                var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", _uploadFolder);
                _logger.LogInformation($"Upload path: {uploadPath}");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                    _logger.LogInformation("Upload folder created");
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadPath, fileName);

                _logger.LogInformation($"Saving file to: {filePath}");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File saved successfully: {fileName}");

                // Return URL for the uploaded file
                var fileUrl = $"/uploads/posts/{fileName}";
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        [Authorize]
        public IActionResult DeleteImage([FromBody] DeleteImageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Url))
                {
                    return BadRequest(new { message = "No URL provided" });
                }

                // Extract filename from URL
                var fileName = Path.GetFileName(request.Url);
                var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", _uploadFolder, fileName);

                // Check if file exists
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    return NotFound(new { message = "File not found" });
                }

                // Delete file
                System.IO.File.Delete(filePath);
                _logger.LogInformation($"File deleted: {fileName}");

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