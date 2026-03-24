using Azure.Storage.Blobs;
using Fan_Website.Infrastructure;

namespace Fan_Website.Service
{
    public class UploadService : IUpload
    {
        private readonly IWebHostEnvironment _env;

        public UploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public BlobContainerClient GetBlobContainer(
            string connectionString,
            string containerName)
        {
            var serviceClient = new BlobServiceClient(connectionString);
            var container = serviceClient.GetBlobContainerClient(containerName);

            container.CreateIfNotExists();

            return container;
        }

        public async Task UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return;

            string fileName = EnsureFileName(file.FileName);
            string path = GetPathAndFileName(fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        // Get the file path and file name
        private string GetPathAndFileName(string filename)
        {
            string path = Path.Combine(_env.WebRootPath, "images");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return Path.Combine(path, filename);
        }

        private string EnsureFileName(string fileName)
        {
            return Path.GetFileName(fileName);
        }
    }
}