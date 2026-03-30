using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Services;
using Fan_Website.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class PostService : IPost
    {
        private readonly AppDbContext context;

        public PostService(AppDbContext ctx)
        {
            context = ctx;
        }

        public async Task Add(Post post)
        {
            context.Add(post);
            await context.SaveChangesAsync();
        }

        public async Task AddReply(PostReply reply)
        {
            context.Add(reply);
            await context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var post = await GetById(id);
            if (post == null) return;

            // FIX: also remove PostImages, consistent with the controller
            if (post.PostImages?.Any() == true)
                context.PostImages.RemoveRange(post.PostImages);

            context.Likes.RemoveRange(context.Likes.Where(l => l.Post.PostId == id));
            context.Replies.RemoveRange(context.Replies.Where(r => r.Post.PostId == id));
            context.Posts.Remove(post);

            await context.SaveChangesAsync();
        }

        public async Task DeleteReply(int id)
        {
            var reply = await GetReplyByIdAsync(id);
            if (reply == null) return;

            context.Remove(reply);
            await context.SaveChangesAsync();
        }

        public async Task EditPost(int id, string newContent, string newTitle)
        {
            var post = await GetById(id);
            if (post == null) return;

            post.Content = newContent;
            post.Title = newTitle;
            post.UpdatedOn = DateTime.UtcNow; 
            context.Posts.Update(post);
            await context.SaveChangesAsync();
        }

        public async Task EditReply(int id, string newContent)
        {
            var reply = await GetReplyByIdAsync(id);
            if (reply == null) return;

            reply.ReplyContent = newContent;
            reply.UpdatedOn = DateTime.UtcNow; 
            context.Replies.Update(reply);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Post>> GetAll()
        {
            return await context.Posts
                .Include(p => p.User)
                .Include(p => p.Replies).ThenInclude(r => r.User)
                .Include(p => p.Forum)
                .Include(p => p.Likes).ThenInclude(l => l.User)
                .ToListAsync();
        }

        public async Task<Post?> GetById(int id)
        {
            return await context.Posts
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
            var query = context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Where(p => p.ForumId == forum.ForumId);

            // FIX: push search filter into DB query instead of fetching all then filtering
            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Content.Contains(searchQuery));

            return await query
                .OrderByDescending(p => p.UpdatedOn)
                .ToListAsync();
        }

        // Global search — FIX: removed duplicate conditions, let DB collation handle case
        public async Task<IEnumerable<Post>> GetFilteredPosts(string searchQuery)
        {
            return await context.Posts
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
            return await context.Posts
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
            var forum = await context.Forums
                .Include(f => f.Posts)
                    .ThenInclude(p => p.User)
                .Where(f => f.ForumId == id)
                .FirstOrDefaultAsync();

            return forum?.Posts ?? Enumerable.Empty<Post>();
        }

        public async Task<PostReply?> GetReplyByIdAsync(int id)
        {
            return await context.Replies
                .Include(r => r.User)
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Post>> GetTopPosts(int n)
        {
            return await context.Posts
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
            return await context.Likes
                .Include(l => l.User)
                .Where(l => l.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Like>> GetAllLikes(int id)
        {
            var post = await context.Posts
                .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                .Where(p => p.PostId == id)
                .FirstOrDefaultAsync();

            return post?.Likes ?? Enumerable.Empty<Like>();
        }

        public async Task<IEnumerable<PostDto>> SearchPostsAsync(string query)
        {
            return await context.Posts
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
            var post = await context.Posts.FindAsync(id);
            if (post == null) return;

            var likeCount = await context.Likes.CountAsync(l => l.Post.PostId == id);
            post.TotalLikes = likeCount;

            await context.SaveChangesAsync();
        }
    }
}