using Fan_Website.Services;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ForumService : IForum
    {
        private readonly AppDbContext _context;

        public ForumService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Create(Forum forum)
        {
            _context.Add(forum);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            _context.Remove(forum);
            await _context.SaveChangesAsync();
        }

        public IQueryable<Forum> Query()
        {
            return _context.Forums.AsNoTracking();
        }

        public Forum GetById(int id)
        {
            return _context.Forums.AsNoTracking().Where(f => f.ForumId == id).FirstOrDefault(); 
        }

        public async Task<Forum> GetByIdAsync(int id)
        {
            return await _context.Forums
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Posts)
                    .ThenInclude(p => p.User)
                .Include(f => f.Posts)
                    .ThenInclude(p => p.Replies).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(f => f.ForumId == id);
        }

        public async Task UpdateForumDescription(int id, string newDescription)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            forum.Description = newDescription;
            _context.Forums.Update(forum);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateForumTitle(int id, string newTitle)
        {
            var forum = await GetByIdAsync(id);
            if (forum is null) return;
            forum.PostTitle = newTitle;
            _context.Forums.Update(forum);
            await _context.SaveChangesAsync();
        }
    }
}