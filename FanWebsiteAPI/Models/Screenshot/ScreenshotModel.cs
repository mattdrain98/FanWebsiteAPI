using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fan_Website.Models.Screenshot
{
    public class ScreenshotModel
    {
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorRating { get; set; }
        public required string ScreenshotImageUrl { get; set; }
        public required int ScreenshotId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string DatePosted { get; set; }
        public required IFormFile ScreenshotUpload { get; set; }
    }
}
