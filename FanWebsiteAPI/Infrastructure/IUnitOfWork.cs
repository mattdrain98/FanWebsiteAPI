using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Fan_Website.Infrastructure
{
    public interface IUnitOfWork
    {
        Task UploadImageAsync(IFormFile file);
    }
}