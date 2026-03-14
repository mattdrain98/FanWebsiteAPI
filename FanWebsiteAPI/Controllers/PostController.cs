using Fan_Website;
using Fan_Website.Models;
using Fan_Website.Models.Posts;
using Fan_Website.Models.Reply;
using Fan_Website.ViewModel;
using FanWebsiteAPI.DTOs;
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
        private readonly ILogger<PostsController> _logger;

        public PostsController(AppDbContext context, ILogger<PostsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create a new post with images
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Get current user
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Verify forum exists
                var forum = await _context.Forums.FindAsync(request.ForumId);
                if (forum == null)
                {
                    return BadRequest(new { message = "Forum not found" });
                }

                // Create post
                var post = new Post
                {
                    Title = request.Title,
                    Content = request.Content,
                    CreatedOn = DateTime.UtcNow,
                    User = user,
                    Forum = forum,
                    TotalLikes = 0,
                    Likes = new List<Like>(),
                    Replies = new List<PostReply>(),
                    PostImages = new List<PostImage>()
                };

                // Add images if provided
                if (request.ImageUrls != null && request.ImageUrls.Count > 0)
                {
                    foreach (var imageUrl in request.ImageUrls)
                    {
                        post.PostImages.Add(new PostImage
                        {
                            Url = imageUrl,
                            CreatedOn = DateTime.UtcNow,
                            Post = post
                        });
                    }
                }

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Post {post.PostId} created with {post.PostImages.Count} images");
                _logger.LogInformation($"Original content: {post.Content}");

                // Now replace temp placeholders with real image IDs in order
                var updatedContent = post.Content;
                var regex = new Regex(@"\[Image-[^\]]+\]");

                // Sort images by ID to ensure consistent order
                var sortedImages = post.PostImages.OrderBy(img => img.Id).ToList();

                foreach (var image in sortedImages.Select((img, idx) => new { img, idx }))
                {
                    var pattern = $@"\[Image-{image.idx}-[^\]]+\]";
                    updatedContent = Regex.Replace(updatedContent, pattern, $"[Image-{image.idx}-{image.img.Id}]");
                }

                _logger.LogInformation($"Updated content: {updatedContent}");

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

                _logger.LogInformation($"Post {post.PostId} created by user {userId}");

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

        /// <summary>
        /// Edit a post (owner only) — supports adding new images
        /// </summary>
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
                post.CreatedOn = DateTime.UtcNow;

                // Add new images if provided
                if (request.NewImageUrls != null && request.NewImageUrls.Count > 0)
                {
                    foreach (var imageUrl in request.NewImageUrls)
                    {
                        post.PostImages?.Add(new PostImage
                        {
                            Url = imageUrl,
                            CreatedOn = DateTime.UtcNow,
                            Post = post
                        });
                    }

                    await _context.SaveChangesAsync();

                    // Replace temp placeholders with real image IDs
                    var updatedContent = post.Content;
                    var sortedImages = post.PostImages?.OrderBy(img => img.Id).ToList();

                    for (int i = 0; i < sortedImages?.Count; i++)
                    {
                        // Replace any temp placeholder for this index
                        var tempPattern = $@"\[Image-{i}-temp\]";
                        updatedContent = Regex.Replace(updatedContent, tempPattern, $"[Image-{i}-{sortedImages[i].Id}]");

                        // Also fix any existing placeholders that may have shifted
                        var existingPattern = $@"\[Image-{i}-\d+\]";
                        updatedContent = Regex.Replace(updatedContent, existingPattern, $"[Image-{i}-{sortedImages[i].Id}]");
                    }

                    post.Content = updatedContent;
                }

                _context.Posts.Update(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Post {id} edited by user {userId}, {request.NewImageUrls?.Count ?? 0} new images added");

                return Ok(new { message = "Post updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing post");
                return StatusCode(500, new { message = "Error editing post", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a post by ID with all images
        /// </summary>
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
                {
                    return NotFound(new { message = "Post not found" });
                }

                // Get current user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool userHasLiked = false; 

                if (post.Likes.Count > 0)
                    userHasLiked = userId != null && post.Likes?.Any(l => l.User.Id == userId) == true;

                var dto = new PostIndexModel
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    PostContent = post.Content,
                    Date = post.CreatedOn,
                    AuthorId = post.User.Id,
                    AuthorName = post.User.UserName ?? "Unknown",
                    AuthorRating = post.User.Rating,
                    AuthorImageUrl = post.User.ImagePath,
                    ForumId = post.ForumId,
                    ForumName = post.Forum.PostTitle,
                    TotalLikes = post.TotalLikes,
                    Likes = post.Likes?.Select(l => new LikeDto
                    {
                        Id = l.Id,
                        User = new LikeUserDto
                        {
                            Id = post.User.Id,
                            UserName = post.User.UserName ?? "Unknown"
                        }
                    }).ToList(),
                    UserHasLiked = userHasLiked,
                    Replies = post.Replies?.OrderByDescending(r => r.CreateOn).Select(r => new PostReplyModel
                    {
                        Id = r.Id,
                        PostTitle = r.Post.Title,
                        PostId = r.Post.PostId,
                        PostContent = r.Post.Content,
                        ForumName = r.Post.Forum.PostTitle,
                        ForumId = r.Post.ForumId,
                        AuthorId = r.User.Id,
                        AuthorName = r.User.UserName ?? "Unknown",
                        AuthorRating = r.User.Rating,
                        AuthorImageUrl = r.User.ImagePath,
                        Date = r.CreateOn,
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

                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Forum)
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .OrderByDescending(p => p.TotalLikes)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(post => new PostListingModel
                    {
                        Id = post.PostId,
                        Title = post.Title,
                        Content = post.Content, 
                        AuthorId = post.User.Id,
                        AuthorName = post.User.UserName ?? "Unknown",
                        AuthorRating = post.User.Rating,
                        AuthorUrl = post.User.ImagePath,
                        TotalLikes = post.TotalLikes,
                        DatePosted = post.CreatedOn.ToString(),
                        RepliesCount = post.Replies.Count,
                        ForumId = post.ForumId,
                        ForumName = post.Forum.PostTitle,
                        PostImages = post.PostImages
                    })
                    .ToListAsync();

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

                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Forum)
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .OrderByDescending(p => p.CreatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(post => new PostListingModel
                    {
                        Id = post.PostId,
                        Title = post.Title,
                        Content = post.Content,
                        AuthorId = post.User.Id,
                        AuthorName = post.User.UserName ?? "Unknown",
                        AuthorRating = post.User.Rating,
                        AuthorUrl = post.User.ImagePath,
                        TotalLikes = post.TotalLikes,
                        DatePosted = post.CreatedOn.ToString(),
                        RepliesCount = post.Replies.Count,
                        ForumId = post.ForumId,
                        ForumName = post.Forum.PostTitle,
                        PostImages = post.PostImages
                    })
                    .ToListAsync();

                return Ok(new { posts, page, totalPages, totalPosts});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest posts");
                return StatusCode(500, new { message = "Error retrieving latest posts", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a post by ID (owner only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var post = await _context.Posts
                    .Include(p => p.PostImages)
                    .Include(p => p.Likes)
                    .Include(p => p.Replies)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                {
                    return NotFound(new { message = "Post not found" });
                }

                // Check ownership using the FK directly
                var postUser = await _context.Users.FirstOrDefaultAsync(u =>
                    _context.Posts.Any(p => p.PostId == id && p.User.Id == u.Id) && u.Id == userId);

                if (postUser == null)
                {
                    return Forbid();
                }

                if (post.PostImages?.Any() == true)
                    _context.PostImages.RemoveRange(post.PostImages);

                if (post.Likes?.Any() == true)
                    _context.Likes.RemoveRange(post.Likes);

                if (post.Replies?.Any() == true)
                    _context.Replies.RemoveRange(post.Replies);

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Post {id} deleted by user {userId}");

                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post");
                return StatusCode(500, new { message = "Error deleting post", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle like on a post (add if not liked, remove if already liked)
        /// </summary>
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
                    .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                    return NotFound(new { message = "Post not found" });

                var existingLike = post.Likes?.FirstOrDefault(l => l.User.Id == userId);

                if (existingLike != null)
                {
                    // Remove like
                    _context.Likes.Remove(existingLike);
                    post.TotalLikes = Math.Max(0, post.TotalLikes - 1);
                }
                else
                {
                    // Add like
                    var like = new Like
                    {
                        User = user,
                        Post = post
                    };
                    _context.Likes.Add(like);
                    post.TotalLikes += 1;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Like toggled on post {id} by user {userId}. Total: {post.TotalLikes}");

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

                var posts = await _context.Likes
                    .Where(l => l.User.Id == userId)
                    .Include(l => l.Post)
                        .ThenInclude(p => p.User)
                    .Include(l => l.Post)
                        .ThenInclude(p => p.Forum)
                    .Include(l => l.Post)
                        .ThenInclude(p => p.PostImages)
                    .Include(l => l.Post)
                        .ThenInclude(p => p.Replies)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new PostListingModel
                    {
                        Id = l.Post.PostId,
                        Title = l.Post.Title,
                        Content = l.Post.Content, 
                        AuthorId = l.Post.User.Id,
                        AuthorName = l.Post.User.UserName ?? "Unknown",
                        AuthorRating = l.Post.User.Rating,
                        AuthorUrl = l.Post.User.ImagePath,
                        TotalLikes = l.Post.TotalLikes,
                        DatePosted = l.Post.CreatedOn.ToString(),
                        RepliesCount = l.Post.Replies.Count,
                        ForumId = l.Post.ForumId,
                        ForumName = l.Post.Forum.PostTitle,
                        PostImages = l.Post.PostImages
                    })
                    .ToListAsync();

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

                var posts = await _context.Posts
                    .Where(p => p.User.Id == userId)
                    .Include(p => p.User)
                    .Include(p => p.Forum)
                    .Include(p => p.PostImages)
                    .Include(p => p.Replies)
                    .OrderByDescending(p => p.CreatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PostListingModel
                    {
                        Id = p.PostId,
                        Title = p.Title,
                        Content = p.Content, 
                        AuthorId = p.User.Id,
                        AuthorName = p.User.UserName ?? "Unknown",
                        AuthorRating = p.User.Rating,
                        AuthorUrl = p.User.ImagePath,
                        TotalLikes = p.TotalLikes,
                        DatePosted = p.CreatedOn.ToString(),
                        RepliesCount = p.Replies.Count,
                        ForumId = p.ForumId,
                        ForumName = p.Forum.PostTitle,
                        PostImages = p.PostImages,

                    })
                    .ToListAsync();

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