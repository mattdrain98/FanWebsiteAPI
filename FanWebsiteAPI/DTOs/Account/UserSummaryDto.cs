namespace FanWebsiteAPI.DTOs.Account
{
    // Lightweight, public-safe summary of a user — used wherever a list of users
    // needs to be shown (newest members, admin user listing) without exposing the
    // full ApplicationUser (IdentityUser) entity or any relationship-specific data.
    public class UserSummaryDto
    {
        public required string Id { get; set; }
        public required string UserName { get; set; }
        public string? ImagePath { get; set; }
        public int Rating { get; set; }
        public required string MemberSince { get; set; }
    }
}
