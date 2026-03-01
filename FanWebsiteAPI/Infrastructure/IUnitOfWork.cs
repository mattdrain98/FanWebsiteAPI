namespace Fan_Website.Infrastructure
{
    public interface IUnitOfWork
    {
        Task UploadImageAsync(IFormFile file);
    }
}