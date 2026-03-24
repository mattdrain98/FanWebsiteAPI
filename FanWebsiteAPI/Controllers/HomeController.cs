using Fan_Website.Infrastructure;
using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Service;
using Fan_Website.Services;
using Fan_Website.ViewModel;
using FanWebsiteAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public HomeController(ILogger<HomeController> logger, IPost postService, IScreenshot screenshotService, IForum forumService, IApplicationUser userManager)
        {
            _logger = logger;
            _postService = postService;
            _screenshotService = screenshotService;
            _forumService = forumService;
            _userManager = userManager; 
        }

        // GET: api/home/stats
        [HttpGet("stats")]
        public ActionResult<HomeStatsDto> GetStats()
        {
            var totalMembers = _userManager.GetAll().Result.Count();
            var totalPosts = _postService.GetLatestPosts(int.MaxValue).Result.Count();
            var totalForums = _forumService.GetTopForums(int.MaxValue).Result.Count();
            var totalReplies = _postService.GetLatestPosts(int.MaxValue)
                                           .Result.Sum(p => p.Replies.Count());

            return Ok(new HomeStatsDto
            {
                TotalMembers = totalMembers,
                TotalPosts = totalPosts,
                TotalForums = totalForums,
                TotalReplies = totalReplies
            });
        }

        // GET: api/home/latest?count=10
        [HttpGet("latest-posts")]
        public ActionResult<IEnumerable<PostListingModel>> GetLatestPosts([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 50) count = 10;

            var posts = _postService.GetLatestPosts(count)
                .Result.Select(post => new PostListingModel
                {
                    Id = post.PostId,
                    Title = post.Title,
                    AuthorName = post.User.UserName ?? "Unkown",
                    AuthorId = post.User.Id,
                    AuthorRating = post.User.Rating,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.CreatedOn.ToString("yyyy-MM-dd HH:mm"),
                    RepliesCount = post.Replies.Count(),
                    Forum = GetForumListingForPost(post),
                    ForumName = post.Forum.PostTitle
                });

            return Ok(posts);
        }

        // GET: api/home/latest?count=10
        [HttpGet("top-forums")]
        public ActionResult<IEnumerable<ForumDto>> GetTopForums([FromQuery] int count = 5)
        {
            if (count <= 0 || count > 50) count = 10;

            var forums = _forumService.GetTopForums(count)
                .Result.Select(forum => new ForumDto
                {
                    ForumId = forum.ForumId,
                    ForumTitle = forum.PostTitle,
                    Description = forum.Description,
                    UserName = forum.User.UserName ?? "Unkown",
                    UserId = forum.User.Id,
                    UserRating = forum.User.Rating,
                    PostsCount = forum.Posts?.Count() ?? 0
                });

            return Ok(forums);
        }

        // GET: api/home/top?count=5&days=7
        // Ranks by TotalLikes descending within the last N days
        [HttpGet("top")]
        public ActionResult<IEnumerable<PostListingModel>> GetTopPosts(
            [FromQuery] int count = 5,
            [FromQuery] int days = 7)
        {
            if (count <= 0 || count > 50) count = 5;
            if (days <= 0 || days > 365) days = 7;

            var since = DateTime.UtcNow.AddDays(-days);

            var posts = _postService.GetLatestPosts(200)   
                .Result.Where(p => p.CreatedOn >= since)
                .OrderByDescending(p => p.TotalLikes)
                .Take(count)
                .Select(post => new PostListingModel
                {
                    Id = post.PostId,
                    Title = post.Title,
                    AuthorName = post.User.UserName ?? "Unkown",
                    AuthorId = post.User.Id,
                    AuthorRating = post.User.Rating,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.CreatedOn.ToString("yyyy-MM-dd HH:mm"),
                    RepliesCount = post.Replies.Count(),
                    Forum = GetForumListingForPost(post),
                    ForumName = post.Forum.PostTitle
                });

            return Ok(posts);
        }

        // GET: api/home/screenshots?count=8
        [HttpGet("screenshots")]
        public ActionResult<IEnumerable<ScreenshotDto>> GetLatestScreenshots([FromQuery] int count = 8)
        {
            if (count <= 0 || count > 50) count = 8;

            var screenshots = _screenshotService.GetAll()
                .Result.OrderByDescending(s => s.CreatedOn)
                .Take(count)
                .Select(s => new ScreenshotDto
                {
                    Id = s.ScreenshotId,
                    Title = s.ScreenshotTitle,
                    Content = s.ScreenshotDescription,
                    AuthorId = s.User.Id,
                    AuthorName = s.User.UserName ?? "Unkown",
                    AuthorRating = s.User.Rating,
                    DatePosted = s.CreatedOn,
                    ImageUrl = s.ImagePath,
                    Slug = s.Slug
                });

            return Ok(screenshots);
        }

        private ForumListingModel GetForumListingForPost(Post post)
        {
            var forum = post.Forum;
            return new ForumListingModel
            {
                ForumId = forum.ForumId,
                ForumTitle = forum.PostTitle,
                Description = forum.Description, 
                AuthorId = forum.User.Id,
                AuthorName = forum.User.UserName ?? "Unkown", 
                AuthorRating = forum.User.Rating
            };
        }

        // GET: api/home/privacy
        [HttpGet("privacy")]
        public ActionResult<string> Privacy()
        {
            return Ok("Privacy info here");
        }

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
