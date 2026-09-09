namespace Fan_Website.Services
{
    public interface IForum
    {
        Task<Forum> GetByIdAsync(int id);
        IQueryable<Forum> Query();

        Task Create(Forum forum);
        Task Delete(int id);
        Task UpdateForumTitle(int id, string newTitle);
        Task UpdateForumDescription(int id, string newDescription);
    }
}