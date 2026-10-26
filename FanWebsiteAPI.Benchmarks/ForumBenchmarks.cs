using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Fan_Website;
using Fan_Website.Controllers;
using Fan_Website.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Benchmarks
{
    /// <summary>
    /// Benchmarks ForumController.GetAllForums (first page). Like the other benchmarks
    /// this uses the EF InMemory provider, so it measures managed query/materialization
    /// overhead, not SQL Server execution time.
    /// Requests are made as an anonymous user (CanModerate = false), which is the
    /// common case and includes the IsHidden filters.
    /// </summary>
    [MemoryDiagnoser]
    public class ForumBenchmarks
    {
        private const int UserCount = 50;
        private const int PostsPerForum = 3;

        private DbContextOptions<AppDbContext> _options = null!;

        [Params(100, 1000)]
        public int ForumCount { get; set; }

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

            foreach (var forum in forums)
            {
                for (var p = 0; p < PostsPerForum; p++)
                {
                    db.Posts.Add(new Post
                    {
                        Title = $"{forum.PostTitle} post {p}",
                        Content = "Content",
                        UpdatedOn = DateTime.UtcNow.AddMinutes(-p),
                        User = users[(forum.ForumId + p) % UserCount],
                        Forum = forum,
                        Replies = new List<PostReply>(),
                        Likes = new List<Like>()
                    });
                }
            }

            db.SaveChanges();
        }

        private (AppDbContext Db, ForumController Controller) CreateController()
        {
            var db = new AppDbContext(_options);
            // GetAllForums doesn't use UserManager, so it's passed as null.
            var controller = new ForumController(
                new ForumService(db),
                new ApplicationUserService(db),
                null!,
                db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
                }
            };
            return (db, controller);
        }

        [Benchmark]
        public async Task GetAllForums()
        {
            var (db, controller) = CreateController();
            await using var _ = db;
            await controller.GetAllForums();
        }
    }
}
