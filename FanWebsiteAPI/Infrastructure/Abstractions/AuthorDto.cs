namespace FanWebsiteAPI.Infrastructure.Abstractions
{
    public abstract class AuthorDto
    {
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public string? AuthorImagePath { get; set; }
        public int AuthorRating { get; set; }
        public required string DatePosted { get; set; }
    }
}