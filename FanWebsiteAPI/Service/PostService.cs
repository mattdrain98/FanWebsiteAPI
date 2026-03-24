using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Services;
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

            context.Likes.RemoveRange(context.Likes.Where(l => l.Post.PostId == id));
            context.Replies.RemoveRange(context.Replies.Where(r => r.Post.PostId == id));

            context.Posts.Remove(post);
            await context.SaveChangesAsync();
        }

        public async Task DeleteReply(int id)
        {
            var reply = await GetReplyByIdAsync(id);

            if (reply != null)
            {
                context.Remove(reply);
                await context.SaveChangesAsync();
            }
        }
        public async Task EditPost(int id, string newContent, string newTitle)
        {
            var post = await GetById(id);

            if (post != null)
            {
                post.Content = newContent;
                post.Title = newTitle;
                post.CreatedOn = DateTime.UtcNow;
                context.Posts.Update(post);
                await context.SaveChangesAsync();
            }
        }

        public async Task EditReply(int id, string newContent)
        {
            var reply = await GetReplyByIdAsync(id);

            if (reply != null)
            {
                reply.ReplyContent = newContent;
                reply.CreateOn = DateTime.UtcNow;
                context.Replies.Update(reply);
                await context.SaveChangesAsync();
            }

        }

        public async Task<IEnumerable<Post>> GetAll()
        {
            return await context.Posts
                .Include(post => post.User)
                .Include(post => post.Replies).ThenInclude(post => post.User)
                .Include(post => post.Forum)
                .Include(post => post.Likes).ThenInclude(like => like.User).ToListAsync();
        }

        public async Task<Post?> GetById(int id)
        {
            return await context.Posts
                .Include(post => post.Forum).ThenInclude(forum => forum.User)
                .Include(post => post.User)
                .Include(post => post.Replies).ThenInclude(reply => reply.User)
                .Include(post => post.Likes).ThenInclude(like => like.User)
                .FirstOrDefaultAsync(post => post.PostId == id);
        }
        public async Task<IEnumerable<Post>> GetFilteredPosts(Forum forum, string searchQuery)
        {
            var posts = await context.Posts.Where(p => p.ForumId == forum.ForumId).ToListAsync();

            if (posts == null) throw new NullReferenceException("There are no posts"); 

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();

                return posts.Where(post =>
                    (post.Title.ToLower().Contains(lowerQuery)) ||
                    (post.Content.ToLower().Contains(lowerQuery)));
            }
            else
                return posts.OrderByDescending(p => p.CreatedOn);
        }

        public async Task<IEnumerable<Post>> GetFilteredPosts(string searchQuery)
        {
            return await context.Posts.Where(post => post.Title.ToLower().Contains(searchQuery) || post.Content.ToLower().Contains(searchQuery) || post.Content.Contains(searchQuery) || post.Title.Contains(searchQuery)).ToListAsync();
        }

        public async Task<IEnumerable<PostListingModel>> SearchPostsAsync(string query)
        {
            return await context.Posts
                .Include(p => p.User)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Include(p => p.Forum)
                    .ThenInclude(f => f.User) 
                .Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(query)) ||
                    (p.Content != null && p.Content.ToLower().Contains(query)))
                .Select(p => new PostListingModel
                {
                    Id = p.PostId,
                    Title = p.Title,

                    AuthorId = p.User.Id,
                    AuthorName = p.User.UserName ?? "Unknown",
                    AuthorRating = p.User != null ? p.User.Rating : 0,

                    DatePosted = p.CreatedOn.ToString(),
                    RepliesCount = p.Replies != null ? p.Replies.Count() : 0,
                    TotalLikes = p.Likes.Count(),

                    Forum = p.Forum != null ? new ForumListingModel
                    {
                        ForumId = p.Forum.ForumId,
                        ForumTitle = p.Forum.PostTitle ?? string.Empty,
                        Description = p.Forum.Description,
                        AuthorId = p.Forum.User.Id,
                        AuthorName = p.Forum.User.UserName ?? "Unknown",
                        AuthorRating = p.Forum.User.Rating
                    } : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetLatestPosts(int n)
        {
            return await context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum).ThenInclude(f => f.User)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedOn)
                .Take(n)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByForum(int id)
        {
            var forum = await context.Forums.Where(forum => forum.ForumId == id).FirstOrDefaultAsync(); 
            return forum.Posts ?? throw new NullReferenceException("Forum is null.");
        }

        public async Task<PostReply?> GetReplyByIdAsync(int id)
        {
            return await context.Replies
                .Include(reply => reply.User)
                .Include(reply => reply.Post)
                .FirstOrDefaultAsync(reply => reply.Id == id);
        }

        public async Task<IEnumerable<Post>> GetTopPosts(int n)
        {
            return await context.Posts
                .Include(p => p.User)
                .Include(p => p.Forum).ThenInclude(f => f.User)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.TotalLikes)
                .Take(n)
                .ToListAsync();
        }


        public async Task UpdatePostLikes(int id)
        {
            var post = await GetById(id);

            if (post != null)
            {
                post.TotalLikes = CalculatePostLikes(post.TotalLikes);
                await context.SaveChangesAsync();
            }
        }

        public int CalculatePostLikes(int likes)
        {
            var inc = 1;
            return likes + inc;
        }

        public async Task<Like?> GetLikeById(int id)
        {
            return await context.Likes.Where(like => like.Id == id)
                .Include(like => like.User)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Like>> GetAllLikes(int id)
        {
            var userLikes = await context.Posts.Where(post => post.PostId == id)
                .Include(post => post.Likes)
                .ThenInclude(like => like.User)
                .FirstOrDefaultAsync();

            return userLikes?.Likes ?? Enumerable.Empty<Like>();
        }
    }
}