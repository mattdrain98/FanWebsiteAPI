using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;

namespace Fan_Website.Infrastructure
{
    public interface IApplicationUser
    {
        Task<ApplicationUser?> GetById(string id);
        Task<IEnumerable<ApplicationUser>> GetAll();
        Task SetProfileImage(string id, Uri uri);
        Task UpdateUserRating(string id, Type type);
        Task<IEnumerable<ApplicationUser>> GetLatestUsers(int n);
        Task<IEnumerable<Follow?>> GetFollowing(string id);
        Task AddComment(ProfileComment comment);
        Task EditProfile(string id, string bio, string username);
        Task<ProfileComment?> GetCommentById(int id);
        Task UpdateComment(ProfileComment comment);
        Task DeleteComment(int id);
    }
}
