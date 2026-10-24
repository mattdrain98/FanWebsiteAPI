using BenchmarkDotNet.Attributes;
using Fan_Website;
using FanWebsiteAPI.Controllers;
using FanWebsiteAPI.Models.Chat;
using FanWebsiteAPI.Models.Report;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Benchmarks
{
    /// <summary>
    /// Baseline for ReportsController.GetReports. Uses the EF InMemory provider, so it
    /// measures managed overhead (query pipeline, materialization, allocations) rather
    /// than SQL Server execution time. Compare before/after an optimization with
    /// the [MemoryDiagnoser] columns as well as Mean.
    /// </summary>
    [MemoryDiagnoser]
    public class GetReportsBenchmarks
    {
        private DbContextOptions<AppDbContext> _options = null!;

        [Params(100, 1000)]
        public int ReportCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"bench-{Guid.NewGuid()}")
                .Options;

            using var db = new AppDbContext(_options);

            var reporter = new ApplicationUser { Id = "reporter", UserName = "reporter" };
            var target = new ApplicationUser { Id = "target", UserName = "target" };
            db.Users.AddRange(reporter, target);

            for (var i = 1; i <= ReportCount; i++)
            {
                var isChat = i % 2 == 0;
                if (isChat)
                {
                    db.ChatMessages.Add(new ChatMessage
                    {
                        Id = i,
                        UserId = target.Id,
                        UserName = target.UserName!,
                        Content = $"message {i}"
                    });
                }

                db.Reports.Add(new Report
                {
                    ReporterId = reporter.Id,
                    TargetUserId = target.Id,
                    ContentType = isChat ? ReportContentType.ChatMessage : ReportContentType.Post,
                    ContentId = i,
                    Reason = "spam",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            db.SaveChanges();
        }

        [Benchmark]
        public async Task GetReports_FirstPage()
        {
            await using var db = new AppDbContext(_options);
            var controller = new ReportsController(db, null!);
            await controller.GetReports();
        }
    }
}
