using Fan_Website;

namespace FanWebsiteAPI.Infrastructure.Abstractions
{
    public abstract class AuditableEntity
    {
        public DateTime CreatedOn { get; set; }
        public required ApplicationUser User { get; set; }
        public required string UserId { get; set; }
    }
}
