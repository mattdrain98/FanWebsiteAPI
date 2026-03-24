namespace Fan_Website.Services
{
    public interface IForum
    {
        Task<Forum> GetByIdAsync(int id);
        Task<IEnumerable<Forum>> GetAll();
        Task Create(Forum forum);
        Task Delete(int id);
        Task UpdateForumTitle(int id, string newTitle);
        Task UpdateForumDescription(int id, string newDescription);
        Task<IEnumerable<Forum>> GetTopForums(int n); 
    }
}