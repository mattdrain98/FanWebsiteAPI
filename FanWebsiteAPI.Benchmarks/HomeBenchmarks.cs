using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Fan_Website;
using Fan_Website.Controllers;
using Fan_Website.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FanWebsiteAPI.Benchmarks
{
    /// <summary>
    /// Benchmarks the endpoints the home page calls (stats, latest posts, top forums,
    /// top posts, screenshots), each on its own plus all five run back to back.
    /// Like the reports benchmark this uses the EF InMemory provider, so it measures
    /// managed query/materialization overhead, not SQL Server execution time.
    /// Requests are made as an anonymous user (CanModerate = false), which is the
    /// common case and includes the IsHidden filters.
    /// </summary>
    [MemoryDiagnoser]
    public class HomeBenchmarks
    {
        private const int UserCount = 50;
        private const int ForumCount = 20;
        private const int RepliesPerPost = 2;

        private DbContextOptions<AppDbContext> _options = null!;

        [Params(100, 1000)]
        public int PostCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"bench-{Guid.NewGuid()}")
                .Options;

            using var db = new AppDbContext(_options);

            var users = Enumerable.Range(1, UserCount)
                .Select(i => new ApplicationUser
                {
                    Id = $"user-{i}",
                    UserName = $"user{i}",
                    Rating = i,
                    IsHidden = i % 10 == 0,
                    MemberSince = DateTime.UtcNow.AddDays(-i)
                })
                .ToList();
            db.Users.AddRange(users);

            var forums = Enumerable.Range(1, ForumCount)
                .Select(i => new Forum
                {
                    PostTitle = $"Forum {i}",
                    Description = $"Description {i}",
                    UpdatedOn = DateTime.UtcNow.AddHours(-i),
                    User = users[i % UserCount]
                })
                .ToList();
            db.Forums.AddRange(forums);

            for (var i = 0; i < PostCount; i++)
            {
                var post = new Post
                {
                    Title = $"Post {i}",
                    Content = $"Content of post {i}",
                    UpdatedOn = DateTime.UtcNow.AddMinutes(-i),
                    TotalLikes = i % 37,
                    User = users[i % UserCount],
                    Forum = forums[i % ForumCount],
                    Replies = new List<PostReply>(),
                    Likes = new List<Like>()
                };

                for (var r = 0; r < RepliesPerPost; r++)
                {
                    post.Replies.Add(new PostReply
                    {
                        ReplyContent = $"Reply {r} to post {i}",
                        UpdatedOn = DateTime.UtcNow.AddMinutes(-i),
                        User = users[(i + r + 1) % UserCount],
                        Post = post
                    });
                }

                db.Posts.Add(post);
            }

            for (var i = 0; i < 50; i++)
            {
                db.Screenshots.Add(new Screenshot
                {
                    ScreenshotTitle = $"Screenshot {i}",
                    ScreenshotDescription = $"Description {i}",
                    ImagePath = $"/uploads/screenshots/{i}.png",
                    UpdatedOn = DateTime.UtcNow.AddMinutes(-i),
                    User = users[i % UserCount]
                });
            }

            db.SaveChanges();
        }

        private (AppDbContext Db, HomeController Controller) CreateController()
        {
            var db = new AppDbContext(_options);
            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                new PostService(db),
                new ScreenshotService(db),
                new ForumService(db),
                new ApplicationUserService(db))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
                }
            };
            return (db, controller);
        }

        [Benchmark]
        public async Task GetStats()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetStats();
        }

        [Benchmark]
        public async Task GetLatestPosts()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetLatestPosts();
        }

        [Benchmark]
        public async Task GetTopForums()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetTopForums();
        }

        [Benchmark]
        public async Task GetTopPosts()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetTopPosts();
        }

        [Benchmark]
        public async Task GetLatestScreenshots()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetLatestScreenshots();
        }

        [Benchmark]
        public async Task HomePage_AllEndpoints()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetStats();
            await controller.GetLatestPosts();
            await controller.GetTopForums();
            await controller.GetTopPosts();
            await controller.GetLatestScreenshots();
        }
    }
}
