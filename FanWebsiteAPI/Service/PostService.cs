using Fan_Website.Models;
using Fan_Website.Models.Forum;
using Fan_Website.Services;
using MailKit.Search;
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
            var post = GetById(id);
            if (post == null) return;

            context.Likes.RemoveRange(context.Likes.Where(l => l.Post.PostId == id));
            context.Replies.RemoveRange(context.Replies.Where(r => r.Post.PostId == id));

            context.Posts.Remove(post);
            await context.SaveChangesAsync();
        }

        public async Task DeleteReply(int id)
        {
            var reply = GetReplyById(id);

            if (reply != null)
            {
                context.Remove(reply);
                await context.SaveChangesAsync();
            }
        }
        public async Task EditPost(int id, string newContent, string newTitle)
        {
            var post = GetById(id);

            if (post != null)
            {
                post.Content = newContent;
                post.Title = newTitle;
                post.CreatedOn = DateTime.Now;
                context.Posts.Update(post);
                await context.SaveChangesAsync();
            }
        }

        public async Task EditReply(int id, string newContent)
        {
            var reply = GetReplyById(id);

            if (reply != null)
            {
                reply.ReplyContent = newContent;
                reply.CreateOn = DateTime.Now;
                context.Replies.Update(reply);
                await context.SaveChangesAsync();
            }

        }

        public IEnumerable<Post> GetAll()
        {
            return context.Posts
                .Include(post => post.User)
                .Include(post => post.Replies).ThenInclude(post => post.User)
                .Include(post => post.Forum)
                .Include(post => post.Likes).ThenInclude(like => like.User);
        }

        public Post? GetById(int id)
        {
            return context.Posts
                .Include(post => post.Forum).ThenInclude(forum => forum.User)
                .Include(post => post.User)
                .Include(post => post.Replies).ThenInclude(reply => reply.User)
                .Include(post => post.Likes).ThenInclude(like => like.User)
                .FirstOrDefault(post => post.PostId == id);
        }
        public IEnumerable<Post> GetFilteredPosts(Forum forum, string searchQuery)
        {
            if (forum.Posts == null) throw new NullReferenceException("There are no posts"); 

            if (string.IsNullOrEmpty(searchQuery))
                return forum.Posts.OrderByDescending(p => p.CreatedOn);

            var lowerQuery = searchQuery.ToLower();

            return forum.Posts.Where(post =>
                (post.Title != null && post.Title.ToLower().Contains(lowerQuery)) ||
                (post.Content != null && post.Content.ToLower().Contains(lowerQuery)));
        }

        public IEnumerable<Post> GetFilteredPosts(string searchQuery)
        {
            return GetAll().Where(post => post.Title.ToLower().Contains(searchQuery) || post.Content.ToLower().Contains(searchQuery) || post.Content.Contains(searchQuery) || post.Title.Contains(searchQuery));
        }

        public async Task<IEnumerable<PostListingModel>> SearchPostsAsync(string query)
        {
            return await context.Posts
                .Include(p => p.User)
                .Include(p => p.Replies)
                .Include(p => p.Likes)
                .Include(p => p.Forum).ThenInclude(f => f.User) 
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

        public IEnumerable<Post> GetLatestPosts(int n)
        {
            return GetAll().OrderByDescending(post => post.CreatedOn).Take(n);
        }

        public IEnumerable<Post> GetPostsByForum(int id)
        {
            return context.Forums.Where(forum => forum.ForumId == id).First().Posts ?? throw new NullReferenceException("Forum is null.");
        }

        public PostReply? GetReplyById(int id)
        {
            return context.Replies
                .Include(reply => reply.User)
                .Include(reply => reply.Post)
                .FirstOrDefault(reply => reply.Id == id);
        }

        public IEnumerable<Post> GetTopPosts(int n)
        {
            return GetAll().OrderByDescending(post => post.TotalLikes).Take(n);
        }

        public async Task UpdatePostLikes(int id)
        {
            var post = GetById(id);

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

        public Like? GetLikeById(int id)
        {
            return context.Likes.Where(like => like.Id == id)
                .Include(like => like.User)
                .FirstOrDefault();
        }

        public IEnumerable<Like> GetAllLikes(int id)
        {
            return context.Posts
                .Where(post => post.PostId == id)
                .Include(post => post.Likes)
                .ThenInclude(like => like.User)
                .FirstOrDefault()
                ?.Likes ?? Enumerable.Empty<Like>();
        }
    }
}