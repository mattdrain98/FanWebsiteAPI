namespace FanWebsiteAPI.Models.Report
{
    public enum ReportContentType
    {
        Post,
        Reply,
        Screenshot,
        ProfileComment,
        User,
        ChatMessage
    }

    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Dismissed
    }

    public class Report
    {
        public int Id { get; set; }
        public required string ReporterId { get; set; }
        public Fan_Website.ApplicationUser Reporter { get; set; } = null!;
        public required string TargetUserId { get; set; }
        public Fan_Website.ApplicationUser TargetUser { get; set; } = null!;
        public ReportContentType ContentType { get; set; }
        public int? ContentId { get; set; }
        public required string Reason { get; set; }
        public string? Details { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedById { get; set; }
        public Fan_Website.ApplicationUser? ReviewedBy { get; set; }
    }
}
