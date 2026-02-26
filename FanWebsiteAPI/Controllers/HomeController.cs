using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Models.Home;
using Fan_Website.Services;
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

        public HomeController(ILogger<HomeController> logger, IPost postService)
        {
            _logger = logger;
            _postService = postService;
        }

        // GET: api/home
        [HttpGet]
        public ActionResult<HomeIndexModel> GetHomeIndex()
        {
            var model = BuildHomeIndexModel();
            return Ok(model);
        }

        private HomeIndexModel BuildHomeIndexModel()
        {
            var latestPosts = _postService.GetLatestPosts(10);

            var posts = latestPosts.Select(post => new PostListingModel
            {
                Id = post.PostId,
                Title = post.Title,
                AuthorName = post.User.UserName,
                AuthorId = post.User.Id,
                AuthorRating = post.User.Rating,
                TotalLikes = post.TotalLikes,
                DatePosted = post.CreatedOn.ToString("yyyy-MM-dd HH:mm"),
                RepliesCount = post.Replies.Count(),
                Forum = GetForumListingForPost(post)
            }).ToList();

            return new HomeIndexModel
            {
                LatestPosts = posts,
                SearchQuery = ""
            };
        }

        private ForumListingModel GetForumListingForPost(Post post)
        {
            var forum = post.Forum;
            return new ForumListingModel
            {
                Id = forum.ForumId,
                Name = forum.PostTitle
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