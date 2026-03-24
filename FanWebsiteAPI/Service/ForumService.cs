using Fan_Website.Services;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ForumService : IForum
    {
        private readonly AppDbContext context;

        public ForumService(AppDbContext ctx)
        {
            context = ctx;
        }

        public async Task Create(Forum forum)
        {
            context.Add(forum);
            await context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            context.Remove(forum);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Forum>> GetAll()
        {
            return await context.Forums
                .Include(forum => forum.User)
                .Include(forum => forum.Posts)
                .ToListAsync();
        }

        public Forum GetById(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Forum> GetByIdAsync(int id)
        {
            return await context.Forums
                .Include(f => f.User)
                .Include(f => f.Posts)
                    .ThenInclude(p => p.User)
                .Include(f => f.Posts)
                    .ThenInclude(p => p.Replies).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(f => f.ForumId == id);
        }

        public async Task<IEnumerable<Forum>> GetTopForums(int n)
        {
            return await context.Forums
                .Include(f => f.User)
                .Include(f => f.Posts)
                .OrderByDescending(f => f.Posts.Count())
                .Take(n)
                .ToListAsync();
        }

        public async Task UpdateForumDescription(int id, string newDescription)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            forum.Description = newDescription;
            context.Forums.Update(forum);
            await context.SaveChangesAsync();
        }

        public async Task UpdateForumTitle(int id, string newTitle)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            forum.PostTitle = newTitle;
            context.Forums.Update(forum);
            await context.SaveChangesAsync();
        }
    }
}