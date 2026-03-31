using Fan_Website;
using FanWebsiteAPI.DTOs.Likes;
using FanWebsiteAPI.DTOs.Posts;
using FanWebsiteAPI.DTOs.Replies;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Models.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService; 
        private readonly ILogger<PostsController> _logger;

        public PostsController(AppDbContext context, ILogger<PostsController> logger, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        // Create a new post with images
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] AddPostDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var forum = await _context.Forums.FindAsync(request.ForumId);
                if (forum == null)
                    return BadRequest(new { message = "Forum not found" });

                var post = new Post
                {
                    Title = request.Title,
                    Content = request.Content,
                    UpdatedOn = DateTime.UtcNow,
                    User = user,
                    Forum = forum,
                    TotalLikes = 0,
                    Likes = new List<Like>(),
                    Replies = new List<PostReply>(),
                    PostImages = new List<PostImage>()
                };

                if (request.ImageUrls != null && request.ImageUrls.Count > 0)
                {
                    foreach (var imageUrl in request.ImageUrls)
                    {
                        post.PostImages.Add(new PostImage
                        {
                            Url = imageUrl,
                            UpdatedOn = DateTime.UtcNow,
                            Post = post
                        });
                    }
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                //_logger.LogInformation("Post {PostId} created with {ImageCount} images", post.PostId, post.PostImages.Count);
                //_logger.LogInformation("Original content: {Content}", post.Content);

                // Replace temp placeholders with real image IDs in order
                var updatedContent = post.Content;
                var sortedImages = post.PostImages.OrderBy(img => img.Id).ToList();

                foreach (var item in sortedImages.Select((img, idx) => new { img, idx }))
                {
                    var pattern = $@"\[Image-{item.idx}-[^\]]+\]";
                    updatedContent = Regex.Replace(updatedContent, pattern, $"[Image-{item.idx}-{item.img.Id}]");
                }

                _logger.LogInformation("Updated content: {Content}", updatedContent);

                if (updatedContent != post.Content)
                {
                    _logger.LogInformation("Content changed, updating post");
                    post.Content = updatedContent;
                    _context.Posts.Update(post);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("Content unchanged");
                }

                _logger.LogInformation("Post {PostId} created by user {UserId}", post.PostId, userId);

                return Ok(new
                {
                    message = "Post created successfully",
                    postId = post.PostId,
                    imageCount = post.PostImages?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, new { message = "Error creating post", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> EditPost(int id, [FromBody] EditPostDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var post = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                if (post.User?.Id != userId)
                    return Forbid();

                post.Title = request.Title;
                post.Content = request.Content ?? "";
                post.UpdatedOn = DateTime.UtcNow;

                if (request.NewImageUrls != null && request.NewImageUrls.Count > 0)
                {
                    foreach (var imageUrl in request.NewImageUrls)
                    {
                        post.PostImages?.Add(new PostImage
                        {
                            Url = imageUrl,
                            UpdatedOn = DateTime.UtcNow,
                            Post = post
                        });
                    }

                    await _context.SaveChangesAsync();

                    var updatedContent = post.Content;
                    var sortedImages = post.PostImages?.OrderBy(img => img.Id).ToList();

                    for (int i = 0; i < sortedImages?.Count; i++)
                    {
                        var tempPattern = $@"\[Image-{i}-temp\]";
                        updatedContent = Regex.Replace(updatedContent, tempPattern, $"[Image-{i}-{sortedImages[i].Id}]");

                        var existingPattern = $@"\[Image-{i}-\d+\]";
                        updatedContent = Regex.Replace(updatedContent, existingPattern, $"[Image-{i}-{sortedImages[i].Id}]");
                    }

                    post.Content = updatedContent;
                }

                _context.Posts.Update(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} edited by user {UserId}, {NewImageCount} new images added",
                    id, userId, request.NewImageUrls?.Count ?? 0);

                return Ok(new { message = "Post updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing post");
                return StatusCode(500, new { message = "Error editing post", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Forum)
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                        .ThenInclude(l => l.User)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userHasLiked = userId != null && post.Likes?.Any(l => l.User.Id == userId) == true;

                var dto = new PostDetailDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Content = post.Content,
                    DatePosted = post.UpdatedOn.ToString("o"),
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.ImagePath,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    TotalLikes = post.TotalLikes,
                    Likes = post.Likes?.Select(l => new LikeDto
                    {
                        Id = l.Id,
                        User = new LikeUserDto
                        {
                            Id = l.User.Id,
                            UserName = l.User.UserName ?? "Unknown"
                        }
                    }).ToList(),
                    UserHasLiked = userHasLiked,
                    Replies = post.Replies?.OrderByDescending(r => r.UpdatedOn).Select(r => new PostReplyDto
                    {
                        Id = r.Id,
                        PostId = post.PostId,
                        AuthorId = r.User.Id,
                        AuthorName = r.User.UserName ?? "Unknown",
                        AuthorRating = r.User.Rating,
                        AuthorImagePath = r.User.ImagePath,
                        DatePosted = r.UpdatedOn.ToString("o"),
                        ReplyContent = r.ReplyContent
                    }).ToList(),
                    PostImages = post.PostImages?.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post");
                return StatusCode(500, new { message = "Error retrieving post", error = ex.Message });
            }
        }

        /// <summary>
        /// Get top posts by likes
        /// </summary>
        [HttpGet("top")]
        public async Task<IActionResult> GetTopPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 6)
        {
            try
            {
                var totalPosts = await _context.Posts.CountAsync();
                var totalPages = Math.Min((int)Math.Ceiling(totalPosts / (double)pageSize), 100);

                var rawPosts = await _context.Posts
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .Include(p => p.Forum)
                    .OrderByDescending(p => p.TotalLikes)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.User)
                    .ToListAsync();

                var posts = rawPosts.Select(post => new PostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.ImagePath,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.UpdatedOn.ToString("o"),
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                }).ToList();

                return Ok(new { posts, page, totalPages, totalPosts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top posts");
                return StatusCode(500, new { message = "Error retrieving top posts", error = ex.Message });
            }
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 6)
        {
            try
            {
                page = Math.Clamp(page, 1, 100);

                var totalPosts = await _context.Posts.CountAsync();
                var totalPages = Math.Min((int)Math.Ceiling(totalPosts / (double)pageSize), 100);

                var rawPosts = await _context.Posts
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .Include(p => p.Forum)
                    .OrderByDescending(p => p.UpdatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.User)
                    .ToListAsync();

                var posts = rawPosts.Select(post => new PostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.ImagePath,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.UpdatedOn.ToString("o"), 
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                }).ToList();

                return Ok(new { posts, page, totalPages, totalPosts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest posts");
                return StatusCode(500, new { message = "Error retrieving latest posts", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var post = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                if (post.User?.Id != userId)
                    return Forbid();

                if (post.PostImages?.Any() == true)
                    _context.PostImages.RemoveRange(post.PostImages);

                if (post.Likes?.Any() == true)
                    _context.Likes.RemoveRange(post.Likes);

                if (post.Replies?.Any() == true)
                    _context.Replies.RemoveRange(post.Replies);

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} deleted by user {UserId}", id, userId);

                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post");
                return StatusCode(500, new { message = "Error deleting post", error = ex.Message });
            }
        }

        // Toggle like on a post (add if not liked, remove if already liked)
        [HttpPost("{id}/likes")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var post = await _context.Posts
                    .Include(p => p.User) 
                    .Include(p => p.Likes)
                        .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var existingLike = post.Likes?.FirstOrDefault(l => l.User.Id == userId);

                if (existingLike != null)
                {
                    _context.Likes.Remove(existingLike);
                    post.TotalLikes = Math.Max(0, post.TotalLikes - 1);
                }
                else
                {
                    var like = new Like { User = user, Post = post };
                    _context.Likes.Add(like);
                    post.TotalLikes += 1;

                    await _notificationService.CreateAsync(
                        post.User.Id,
                        $"{user.UserName} liked your post \"{post.Title}\"",
                        "like",
                        $"/post/{post.PostId}"
                    );
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Like toggled on post {PostId} by user {UserId}. Total: {TotalLikes}",
                    id, userId, post.TotalLikes);

                return Ok(post.TotalLikes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like");
                return StatusCode(500, new { message = "Error toggling like", error = ex.Message });
            }
        }

        [HttpGet("liked")]
        [Authorize]
        public async Task<IActionResult> GetLikedPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 6)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                page = Math.Clamp(page, 1, 100);

                var totalLikedPosts = await _context.Likes.CountAsync(l => l.User.Id == userId);
                var totalPages = Math.Min((int)Math.Ceiling(totalLikedPosts / (double)pageSize), 100);

                var rawPosts = await _context.Posts
                    .Include(p => p.PostImages)
                    .Include(p => p.Replies)
                    .Include(p => p.Forum)
                    .OrderByDescending(p => p.UpdatedOn)
                    .Where(p => p.Likes.Any(l => l.User.Id == userId))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.User)
                    .ToListAsync(); 

                var posts = rawPosts.Select(post => new PostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.ImagePath,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.UpdatedOn.ToString("o"), 
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                }).ToList();

                return Ok(new { posts, page, totalPages, totalLikedPosts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving liked posts");
                return StatusCode(500, new { message = "Error retrieving liked posts", error = ex.Message });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 6)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                page = Math.Clamp(page, 1, 100);

                var totalUserPosts = await _context.Posts.CountAsync(p => p.User.Id == userId);
                var totalPages = Math.Min((int)Math.Ceiling(totalUserPosts / (double)pageSize), 100);

                var rawPosts = await _context.Posts
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .Include(p => p.Forum)
                    .OrderByDescending(p => p.UpdatedOn)
                    .Where(p => p.User.Id == userId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.User)
                    .ToListAsync();

                var posts = rawPosts.Select(post => new PostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImagePath = post.User.ImagePath,
                    TotalLikes = post.TotalLikes,
                    DatePosted = post.UpdatedOn.ToString("o"), 
                    RepliesCount = post.Replies.Count,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    PostImages = post.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                }).ToList();

                return Ok(new { posts, page, totalPages, totalUserPosts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user posts");
                return StatusCode(500, new { message = "Error retrieving user posts", error = ex.Message });
            }
        }
    }
}