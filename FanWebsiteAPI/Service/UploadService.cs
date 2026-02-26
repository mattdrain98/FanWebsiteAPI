using Azure.Storage.Blobs;
using Fan_Website.Infrastructure;

namespace Fan_Website.Service
{
    public class UploadService : IUpload
    {
        public BlobContainerClient GetBlobContainer(
            string connectionString,
            string containerName)
        {
            var serviceClient = new BlobServiceClient(connectionString);
            var container = serviceClient.GetBlobContainerClient(containerName);

            container.CreateIfNotExists();

            return container;
        }
    }
}