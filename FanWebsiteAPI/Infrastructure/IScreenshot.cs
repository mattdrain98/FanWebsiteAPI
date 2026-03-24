namespace Fan_Website.Infrastructure
{
    public interface IScreenshot
    {
        Task<Screenshot?> GetById(int id);
        Task SetScreenshotImage(int id, Uri uri);
        Task Add(Screenshot screenshot);
        Task<IEnumerable<Screenshot>> GetAll(); 
        Task<IEnumerable<Screenshot>> GetLatestScreenshots(int n);
        Task<IEnumerable<ApplicationUser>> GetAllUsers();
        Task Delete(int id);
        Task<ApplicationUser?> GetUserById(string id); //Technically can be null, but chances are there will always be a user 
    }
}
