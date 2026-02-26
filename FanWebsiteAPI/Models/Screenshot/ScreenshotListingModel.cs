namespace Fan_Website.Models.Screenshot
{
    public class ScreenshotListingModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string Author { get; set; }
        public required string AuthorName { get; set; }


        public required int AuthorRating { get; set; }
        public required string AuthorId { get; set; }
        public required string DatePosted { get; set; }
        public required string ImageUrl { get; set; }
    }
}
