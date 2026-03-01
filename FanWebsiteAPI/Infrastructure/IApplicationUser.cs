using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;

namespace Fan_Website.Infrastructure
{
    public interface IApplicationUser
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        IEnumerable<ApplicationUser> GetAll();
        Task SetProfileImage(string id, Uri uri);
        Task UpdateUserRating(string id, Type type);
        IEnumerable<ApplicationUser> GetLatestUsers(int n);
        Task<IEnumerable<Follow>> GetFollowingAsync(string id);
        Task AddComment(ProfileComment comment);
        Task EditProfile(string id, string bio, string username); 
    }
}
