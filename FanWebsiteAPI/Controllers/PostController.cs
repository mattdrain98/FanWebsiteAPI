using Fan_Website.Infrastructure;
using Fan_Website.Models;
using Fan_Website.Models.Post;
using Fan_Website.Models.Reply;
using Fan_Website.Services;
using FanWebsiteAPI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Fan_Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPost postService;
        private readonly IForum forumService;
        private readonly IApplicationUser userService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext context;

        public PostController(IPost _postService, IForum _forumService, IApplicationUser _userService, UserManager<ApplicationUser> _userManager, AppDbContext ctx)
        {
            postService = _postService;
            forumService = _forumService;
            userService = _userService;
            userManager = _userManager;
            context = ctx;
        }

        // GET api/post/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PostIndexModel>> GetPost(int id)
        {
            var post = postService.GetById(id);
            if (post == null) return NotFound();

            var replies = BuildPostReplies(post.Replies);
            var userId = userManager.GetUserId(User);

            var model = new PostIndexModel
            {
                Id = post.PostId,
                Title = post.Title,
                AuthorName = post.User.UserName,
                AuthorId = post.User.Id,
                AuthorRating = post.User.Rating,
                AuthorImageUrl = post.User.ImagePath,
                Date = post.CreatedOn,
                PostContent = post.Content,
                Replies = replies,
                TotalLikes = post.Likes.Count(),
                Likes = post.Likes.Select(l => new LikeDto
                {
                    Id = l.Id,
                    User = new LikeUserDto
                    {
                        Id = l.User.Id,
                        UserName = l.User.UserName
                    }
                }),
                ForumId = post.Forum.ForumId,
                ForumName = post.Forum.PostTitle,
                UserHasLiked = post.Likes.Any(l => l.User.Id == userId)
            };
            return Ok(model);
        }

        // GET api/post/user
        [HttpGet("user")]
        public ActionResult<IEnumerable<PostListingModel>> GetUserPosts()
        {
            var username = User.Identity.Name;
            var posts = postService.GetAll()
                .Where(p => p.User.UserName == username)
                .Select(post => new PostListingModel
                {
                    Id = post.PostId,
                    Title = post.Title,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName,
                    AuthorRating = post.User.Rating,
                    DatePosted = post.CreatedOn.ToString(),
                    ForumId = post.Forum.ForumId,
                    ForumName = post.Forum.PostTitle,
                    TotalLikes = post.Likes.Count(),
                    RepliesCount = post.Replies.Count()
                }); 
            return Ok(posts);
        }

        // POST api/post
        [HttpPost]
        public async Task<ActionResult<PostIndexModel>> AddPost([FromBody] NewPostModel model)
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.FindByIdAsync(userId);

            var post = BuildPost(model, user);
            await postService.Add(post);
            await userService.UpdateUserRating(userId, typeof(Post));

            var response = new PostIndexModel
            {
                Id = post.PostId,
                Title = post.Title,
                AuthorName = user.UserName,
                AuthorId = user.Id,
                AuthorRating = user.Rating,
                AuthorImageUrl = user.ImagePath,
                Date = post.CreatedOn,
                PostContent = post.Content,
                TotalLikes = post.TotalLikes,
                ForumId = post.Forum.ForumId,
                ForumName = post.Forum.PostTitle
            };
            return CreatedAtAction(nameof(GetPost), new { id = post.PostId }, response);
        }

        // POST api/post/search
        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<PostListingModel>>> Search([FromBody] PostTopicModel model)
        {
            model.Posts = await postService.SearchPostsAsync(model.SearchQuery);
            return Ok(model.Posts);
        }

        // POST api/post/{id}/likes
        [HttpPost("{id}/likes")]
        public ActionResult<int> UpdateLikes(int id)
        {
            var post = postService.GetById(id);
            if (post == null) return NotFound();

            var userId = userManager.GetUserId(User);
            var user = userService.GetById(userId);

            var like = new Like { User = user, Post = post };
            var likes = post.Likes.ToList();

            var userLike = likes.FirstOrDefault(l => l.User.Id == user.Id);
            if (userLike != null)
            {
                context.Likes.Remove(userLike);
            }
            else
            {
                context.Likes.Add(like);
            }

            context.SaveChanges();
            post.TotalLikes = post.Likes.Count();
            context.Posts.Update(post);
            context.SaveChanges();

            return Ok(post.TotalLikes);
        }

        // PUT api/post/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> EditPost(int id, [FromBody] PostEditModel model)
        {
            var post = postService.GetById(id);
            if (post == null) return NotFound();

            await postService.EditPost(id, model.Content, model.Title);
            return NoContent();
        }

        // DELETE api/post/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePost(int id)
        {
            var post = postService.GetById(id);
            if (post == null) return NotFound();

            await postService.Delete(id);
            return NoContent();
        }

        // Helpers
        private Post BuildPost(NewPostModel model, ApplicationUser user)
        {
            var forum = forumService.GetById(model.ForumId);
            return new Post
            {
                Title = model.Title,
                Content = model.Content,
                CreatedOn = DateTime.Now,
                User = user,
                Forum = forum,
                TotalLikes = 0
            };
        }

        private IEnumerable<PostReplyModel> BuildPostReplies(IEnumerable<PostReply> replies)
        {
            return replies.Select(reply => new PostReplyModel
            {
                Id = reply.Id,
                AuthorImageUrl = reply.User.ImagePath ?? null,
                AuthorName = reply.User.UserName,
                AuthorId = reply.User.Id,
                AuthorRating = reply.User.Rating,
                Date = reply.CreateOn,
                ReplyContent = reply.Content,
                PostId = reply.Post.PostId,
                PostContent = reply.Post.Content,
                PostTitle = reply.Post.Title,
                ForumId = reply.Post.Forum.ForumId,
                ForumName = reply.Post.Forum.PostTitle
            });
        }

        [HttpGet("top")]
        public ActionResult<IEnumerable<PostListingModel>> GetTopPosts(int count = 10)
        {
            var posts = postService.GetAll()
                .OrderByDescending(p => p.Likes.Count())
                .ThenByDescending(p => p.CreatedOn)
                .Take(count)
                .Select(post => new PostListingModel
                {
                    Id = post.PostId,
                    Title = post.Title,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName,
                    AuthorRating = post.User.Rating,
                    DatePosted = post.CreatedOn.ToString(),
                    ForumId = post.Forum.ForumId,
                    ForumName = post.Forum.PostTitle,
                    TotalLikes = post.Likes.Count(),
                    RepliesCount = post.Replies.Count()
                });

            return Ok(posts);
        }
    }
}