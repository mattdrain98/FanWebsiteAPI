using System.ComponentModel.DataAnnotations;

namespace Fan_Website
{
    public class Screenshot
    {

        public required int ScreenshotId { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        public required string ScreenshotTitle { get; set; }
        public required string ImagePath { get; set; }
        public required  string ScreenshotDescription { get; set; }
        public required DateTime CreatedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public string? Slug =>
            ScreenshotTitle?.Replace(' ', '-').ToLower();
    }
}
