using Fan_Website.Models.Follow;
using Fan_Website.Models.ProfileComment;

namespace Fan_Website.Infrastructure
{
    public interface IApplicationUser
    {
        ApplicationUser? GetById(string id);
        IEnumerable<ApplicationUser> GetAll();
        Task SetProfileImage(string id, Uri uri);
        Task UpdateUserRating(string id, Type type);
        IEnumerable<ApplicationUser> GetLatestUsers(int n);
        IEnumerable<Follow> GetFollowing(string id);
        Task AddComment(ProfileComment comment);
        Task EditProfile(string id, string bio, string username);
        Task<ProfileComment> GetCommentById(int id);
        Task UpdateComment(ProfileComment comment);
        Task DeleteComment(int id);
    }
}
