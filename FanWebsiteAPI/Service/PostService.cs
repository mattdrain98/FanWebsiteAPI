using Fan_Website.Services;
using FanWebsiteAPI.DTOs.Posts;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class PostService : IPost
    {
        private readonly AppDbContext _context;

        public PostService(AppDbContext ctx)
        {
            _context = ctx;
        }

        public async Task Add(Post post)
        {
            _context.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task AddReply(PostReply reply)
        {
            _context.Add(reply);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var post = await GetById(id);
            if (post == null) return;

            if (post.PostImages?.Any() == true)
                _context.PostImages.RemoveRange(post.PostImages);

            _context.Likes.RemoveRange(_context.Likes.Where(l => l.Post.PostId == id));
            _context.Replies.RemoveRange(_context.Replies.Where(r => r.Post.PostId == id));
            _context.Posts.Remove(post);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteReply(int id)
        {
            var reply = await GetReplyByIdAsync(id);
            if (reply == null) return;

            _context.Remove(reply);
            await _context.SaveChangesAsync();
        }

        public async Task EditPost(int id, string newContent, string newTitle)
        {
            var post = await GetById(id);
            if (post == null) return;

            post.Content = newContent;
            post.Title = newTitle;
            post.UpdatedOn = DateTime.UtcNow; 
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task EditReply(int id, string newContent)
        {
            var reply = await GetReplyByIdAsync(id);
            if (reply == null) return;

            reply.ReplyContent = newContent;
            reply.UpdatedOn = DateTime.UtcNow; 
            _context.Replies.Update(reply);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Post>> GetAll()
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Replies).ThenInclude(r => r.User)
                .Include(p => p.Forum)
                .Include(p => p.Likes).ThenInclude(l => l.User)
                .ToListAsync();
        }

        public async Task<Post?> GetById(int id)
        {
            return await _context.Posts
                .Include(p => p.Forum).ThenInclude(f => f.User)
                .Include(p => p.User)
                .Include(p => p.PostImages)
                .Include(p => p.Replies).ThenInclude(r => r.User)
                .Include(p => p.Likes).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(p => p.PostId == id);
        }

        // Forum-scoped filtered search — filter and search in the query, not in memory
        public async Task<IEnumerable<Post>> GetFilteredPosts(Forum forum, string searchQuery)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Where(p => p.ForumId == forum.ForumId);

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Content.Contains(searchQuery));

            return await query
                .OrderByDescending(p => p.UpdatedOn)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetFilteredPosts(string searchQuery)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Content.Contains(searchQuery))
                .OrderByDescending(p => p.UpdatedOn)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetLatestPosts(int n)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum).ThenInclude(f => f.User)
                .Include(p => p.PostImages)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.UpdatedOn)
                .Take(n)
                .ToListAsync();
        }

        // FIX: include Posts in the query so forum.Posts isn't null
        public async Task<IEnumerable<Post>> GetPostsByForum(int id)
        {
            var forum = await _context.Forums
                .Include(f => f.Posts)
                    .ThenInclude(p => p.User)
                .Where(f => f.ForumId == id)
                .FirstOrDefaultAsync();

            return forum?.Posts ?? Enumerable.Empty<Post>();
        }

        public async Task<PostReply?> GetReplyByIdAsync(int id)
        {
            return await _context.Replies
                .Include(r => r.User)
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Post>> GetTopPosts(int n)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum).ThenInclude(f => f.User)
                .Include(p => p.PostImages)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.TotalLikes)
                .Take(n)
                .ToListAsync();
        }

        public async Task<Like?> GetLikeById(int id)
        {
            return await _context.Likes
                .Include(l => l.User)
                .Where(l => l.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Like>> GetAllLikes(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                .Where(p => p.PostId == id)
                .FirstOrDefaultAsync();

            return post?.Likes ?? Enumerable.Empty<Like>();
        }

        public async Task<IEnumerable<PostDto>> SearchPostsAsync(string query)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.PostImages)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Where(p =>
                    p.Title.Contains(query) ||
                    p.Content.Contains(query))
                .OrderByDescending(p => p.UpdatedOn)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Content = p.Content,
                    AuthorId = p.User.Id,
                    AuthorName = p.User.UserName ?? "Unknown",
                    AuthorRating = p.User.Rating,
                    AuthorImagePath = p.User.ImagePath,
                    TotalLikes = p.TotalLikes,
                    DatePosted = p.UpdatedOn.ToString(),
                    RepliesCount = p.Replies.Count,
                    ForumId = p.ForumId,
                    ForumName = p.Forum.PostTitle,
                    PostImages = p.PostImages.Select(img => new PostImageDto
                    {
                        Id = img.Id,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task UpdatePostLikes(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return;

            var likeCount = await _context.Likes.CountAsync(l => l.Post.PostId == id);
            post.TotalLikes = likeCount;

            await _context.SaveChangesAsync();
        }
    }
}