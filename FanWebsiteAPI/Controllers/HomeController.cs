using Fan_Website.Infrastructure;
using Fan_Website.Models;
using Fan_Website.Services;
using FanWebsiteAPI.DTOs.Forums;
using FanWebsiteAPI.DTOs.Home;
using FanWebsiteAPI.DTOs.Posts;
using FanWebsiteAPI.DTOs.Screenshots;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPost _postService;
        private readonly IScreenshot _screenshotService;
        private readonly IForum _forumService;
        private readonly IApplicationUser _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            IPost postService,
            IScreenshot screenshotService,
            IForum forumService,
            IApplicationUser userManager)
        {
            _logger = logger;
            _postService = postService;
            _screenshotService = screenshotService;
            _forumService = forumService;
            _userManager = userManager;
        }

        // GET: api/home/stats
        [HttpGet("stats")]
        public async Task<ActionResult<HomeStatsDto>> GetStats()
        {
            var members = await _userManager.GetAll();
            var posts = await _postService.GetLatestPosts(int.MaxValue);
            var postList = posts.ToList();
            var totalForums = await _forumService.Query().CountAsync();

            return Ok(new HomeStatsDto
            {
                TotalMembers = members.Count(),
                TotalPosts = postList.Count,
                TotalForums = totalForums,
                TotalReplies = postList.Sum(p => p.Replies.Count)
            });
        }

        // GET: api/home/latest-posts?count=10
        [HttpGet("latest-posts")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetLatestPosts([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 50) count = 10;

            var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
            var posts = await _postService.GetLatestPosts(count);

            var result = posts.Where(post => canModerate || !post.User.IsHidden).Select(post => new PostDto
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                AuthorName = post.User.UserName ?? "Unknown",
                AuthorId = post.User.Id,
                AuthorRating = post.User.Rating,
                AuthorImagePath = post.User.IsHidden ? null : post.User.ImagePath,
                TotalLikes = post.TotalLikes,
                DatePosted = post.UpdatedOn.ToString("yyyy-MM-dd HH:mm"),
                RepliesCount = post.Replies.Count(r => canModerate || !r.User.IsHidden),
                ForumId = post.ForumId,          
                ForumName = post.Forum.PostTitle  
            });

            return Ok(result);
        }

        // GET: api/home/top-forums?count=5
        [HttpGet("top-forums")]
        public async Task<ActionResult<IEnumerable<ForumDto>>> GetTopForums([FromQuery] int count = 5)
        {
            if (count <= 0 || count > 50) count = 5;

            var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");

            var result = await _forumService.Query()
                .Where(forum => canModerate || !forum.User.IsHidden)
                .OrderByDescending(forum => forum.Posts.Count())
                .Take(count)
                .Select(forum => new ForumDto
                {
                    ForumId = forum.ForumId,
                    ForumTitle = forum.PostTitle,
                    Description = forum.Description,
                    AuthorName = forum.User.UserName ?? "Unknown",
                    AuthorId = forum.User.Id,
                    AuthorRating = forum.User.Rating,
                    AuthorImagePath = forum.User.IsHidden ? null : forum.User.ImagePath,
                    DatePosted = forum.UpdatedOn.ToString(),
                    PostsCount = forum.Posts.Count(p => canModerate || !p.User.IsHidden)
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/home/top?count=5&days=7
        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetTopPosts(
            [FromQuery] int count = 5,
            [FromQuery] int days = 7)
        {
            if (count <= 0 || count > 50) count = 5;
            if (days <= 0 || days > 365) days = 7;

            var canModerate = User.IsInRole("Admin") || User.IsInRole("Moderator");
            var since = DateTime.UtcNow.AddDays(-days);
            var posts = await _postService.GetLatestPosts(200);

            var result = posts
                .Where(p => p.UpdatedOn >= since && (canModerate || !p.User.IsHidden))
                .OrderByDescending(p => p.TotalLikes)
                .Take(count)
                .Select(post => new PostDto
                {
                    PostId = post.PostId, 
                    Title = post.Title,
                    Content = post.Content,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorId = post.User.Id,
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.IsHidden ? null : post.User.ImagePath,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.UpdatedOn.ToString("yyyy-MM-dd HH:mm"),
                    RepliesCount = post.Replies.Count(r => canModerate || !r.User.IsHidden),
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle
                });

            return Ok(result);
        }

        // GET: api/home/screenshots?count=8
        [HttpGet("screenshots")]
        public async Task<ActionResult<IEnumerable<ScreenshotDto>>> GetLatestScreenshots(
            [FromQuery] int count = 8)
        {
            if (count <= 0 || count > 50) count = 8;

            var result = await _screenshotService.Query()
                .OrderByDescending(s => s.UpdatedOn)
                .Take(count)
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName ?? "Unknown",
                    AuthorRating = s.User.Rating,
                    DatePosted = s.UpdatedOn.ToString(),
                    ImageUrl = s.ImagePath,
                    Slug = s.Slug
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/home/privacy
        [HttpGet("privacy")]
        public ActionResult<string> Privacy() => Ok("Privacy info here");

        // GET: api/home/error
        [HttpGet("error")]
        public ActionResult<ErrorViewModel> Error()
        {
            return Ok(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}