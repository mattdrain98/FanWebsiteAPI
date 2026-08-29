using FanWebsiteAPI.Models.Report;

namespace FanWebsiteAPI.DTOs.Reports
{
    public class CreateReportDto
    {
        public required string TargetUserId { get; set; }
        public ReportContentType ContentType { get; set; }
        public int? ContentId { get; set; }
        public required string Reason { get; set; }
        public string? Details { get; set; }
    }

    public class ReportDto
    {
        public int Id { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetUserName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int? ContentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
    }

    public class ReviewReportDto
    {
        public required string Action { get; set; } // "reviewed" or "dismissed"
        public string? Note { get; set; }
    }
}
