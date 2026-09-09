using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ApplicationUserService : IApplicationUser
    {
        private readonly AppDbContext _context;

        public ApplicationUserService(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ApplicationUser> Query()
        {
            return _context.ApplicationUsers.AsNoTracking();
        }

        public async Task<IEnumerable<ApplicationUser>> GetAll()
        {
            return await Query().ToListAsync();
        }

        public async Task<ApplicationUser?> GetById(string id)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.ProfileComments).ThenInclude(c => c.CommentUser)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateUserRating(string userId, Type type)
        {
            var user = await GetById(userId);

            if (user != null)
            {
                user.Rating = CalculateUserRating(type, user.Rating);
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
        }

        private int CalculateUserRating(Type type, int userRating)
        {
            var inc = type switch
            {
                _ when type == typeof(Post) => 1,
                _ when type == typeof(Screenshot) => 2,
                _ when type == typeof(Forum) => 2,
                _ when type == typeof(PostReply) => 3,
                _ when type == typeof(ProfileComment) => 3,
                _ => 0
            };

            return userRating + inc;
        }

        public async Task SetProfileImage(string id, Uri uri)
        {
            var user = await GetById(id);
            user.ImagePath = uri.AbsoluteUri;
            _context.Update(user);
            await _context.SaveChangesAsync(); 
        }

        public async Task<IEnumerable<ApplicationUser>> GetLatestUsers(int n)
        {
            var users = await _context.ApplicationUsers.AsNoTracking().OrderByDescending(u => u.MemberSince).Take(n).ToListAsync();
            foreach (var user in users.Where(u => u.IsHidden))
            {
                user.ImagePath = null;
            }
            return users;
        }

        public async Task<ProfileComment?> GetCommentById(int id)
        {
            return await _context.ProfileComments
                .AsNoTracking()
                .Include(c => c.CommentUser)
                .Include(c => c.ProfileUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task UpdateComment(ProfileComment comment)
        {
            _context.ProfileComments.Update(comment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteComment(int id)
        {
            var comment = await _context.ProfileComments.FindAsync(id);
            if (comment == null) return;
            _context.ProfileComments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Follow?>> GetFollowing(string id)
        {
            var user = await GetById(id);
            return await _context.Follows
                .AsNoTracking()
                .Where(f => f.Follower == user)
                .ToListAsync();
        }

        public async Task AddComment(ProfileComment comment)
        {
            _context.Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task EditProfile(string id, string bio, string username)
        {
            var user = await GetById(id);
            user.UserName = username;
            user.NormalizedUserName = username.ToUpper();
            user.Bio = bio;
            _context.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
