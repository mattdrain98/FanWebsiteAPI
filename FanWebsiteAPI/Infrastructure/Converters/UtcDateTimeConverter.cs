using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FanWebsiteAPI.Infrastructure.Converters
{
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }
}
