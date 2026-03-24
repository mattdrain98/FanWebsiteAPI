using Azure.Storage.Blobs;

namespace Fan_Website.Infrastructure
{
    public interface IUpload
    {
        BlobContainerClient GetBlobContainer(
            string connectionString,
            string containerName);
        Task UploadImageAsync(IFormFile file);
    }
}