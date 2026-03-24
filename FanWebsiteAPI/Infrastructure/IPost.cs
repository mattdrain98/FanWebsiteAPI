using Fan_Website.Models;

namespace Fan_Website.Services
{
    public interface IPost
    {
        Task<Post?> GetById(int id);
        Task<IEnumerable<Post>> GetAll();
        Task<IEnumerable<Post>> GetFilteredPosts(Forum forum, string searchQuery);
        Task<IEnumerable<Post>> GetFilteredPosts(string searchQuery);
        Task<IEnumerable<Post>> GetPostsByForum(int id);
        Task<IEnumerable<Post>> GetLatestPosts(int n);
        Task<IEnumerable<PostListingModel>> SearchPostsAsync(string query);
        Task<IEnumerable<Post>> GetTopPosts(int likes); 
        Task Add(Post post);
        Task Delete(int id);
        Task EditPost(int id, string newContent, string newTitle);
        Task AddReply(PostReply reply);
        Task EditReply(int id, string newContent); 
        Task DeleteReply(int id);
        Task UpdatePostLikes(int likes);
        Task<PostReply?> GetReplyByIdAsync(int id);
        Task<Like?> GetLikeById(int id);
        Task<IEnumerable<Like>> GetAllLikes(int id);
    }
}
