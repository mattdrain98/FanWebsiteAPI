using Fan_Website.Infrastructure;
using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ApplicationUserService : IApplicationUser
    {
        private readonly AppDbContext context; 

        public ApplicationUserService(AppDbContext ctx)
        {
            context = ctx; 
        }
        public IEnumerable<ApplicationUser> GetAll()
        {
            return context.ApplicationUsers; 
        }

        public ApplicationUser GetById(string id)
        {
            var user = context.Users
        .Include(u => u.Follows).ThenInclude(f => f.Follower)
        .Include(u => u.Followings).ThenInclude(f => f.Following)
        .Include(u => u.ProfileComments)
        .FirstOrDefault(u => u.Id == id);

            return user ?? throw new KeyNotFoundException($"User with ID {id} was not found.");
        }

        public async Task UpdateUserRating(string userId, Type type)
        {
            var user = GetById(userId);
            user.Rating = CalculateUserRating(type, user.Rating);
            await context.SaveChangesAsync(); 
        }

        private int CalculateUserRating(Type type, int userRating)
        {
            var inc = 0;

            if (type == typeof(Post))
            {
                inc = 1;
            }

            if (type == typeof(PostReply))
            {
                inc = 3;
            }
            if (type == typeof(Screenshot))
            {
                inc = 2;
            }
            if (type == typeof(Forum))
            {
                inc = 2; 
            }
            if (type == typeof(ProfileComment))
            {
                inc = 3;
            }

            return userRating + inc; 
        }

        public async Task SetProfileImage(string id, Uri uri)
        {
            var user = GetById(id);
            user.ImagePath = uri.AbsoluteUri;
            context.Update(user);
            await context.SaveChangesAsync(); 
        }

        public IEnumerable<ApplicationUser> GetLatestUsers(int n)
        {
            return GetAll().OrderByDescending(user => user.MemberSince).Take(n);
        }

        public async Task<ProfileComment> GetCommentById(int id)
        {
            return await context.ProfileComments
                .Include(c => c.CommentUser)
                .Include(c => c.ProfileUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        [HttpPut("profilecomment/{id:int}")]
        public async Task UpdateComment(ProfileComment comment)
        {
            context.ProfileComments.Update(comment);
            await context.SaveChangesAsync();
        }

        [HttpDelete("profilecomment/{id:int}")]
        public async Task DeleteComment(int id)
        {
            var comment = await context.ProfileComments.FindAsync(id);
            if (comment == null) return;
            context.ProfileComments.Remove(comment);
            await context.SaveChangesAsync();
        }

        public IEnumerable<Follow> GetFollowing(string id)
        { 
            var user = GetById(id);
            var following = context.Follows.Where(follow => follow.Following == user) ?? null;
            return following; 
        }
        public async Task AddComment(ProfileComment comment)
        {
            context.Add(comment);
            await context.SaveChangesAsync();
        }

        public async Task EditProfile(string id, string bio, string username)
        {
            var user = GetById(id);
            user.UserName = username;
            context.Update(user);
            await context.SaveChangesAsync();
            user.NormalizedUserName = username.ToUpper();
            context.Update(user);
            await context.SaveChangesAsync();
            user.Bio = bio;
            context.Update(user);
            await context.SaveChangesAsync(); 
        }
    }
}
