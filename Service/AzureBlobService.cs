
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Service
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureStorage:ConnectionString"];

            var containerName =
                configuration["AzureStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(connectionString) ||
                string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException(
                    "Azure Storage configuration is missing.");
            }

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            var extension = Path.GetExtension(fileName);

            var blobName =
                $"{Guid.NewGuid()}{extension}";

            var blobClient =
                _containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Content, string ContentType)>
    DownloadImageAsync(string blobUrl)
        {
            var blobUri = new Uri(blobUrl);

            var blobName = Uri.UnescapeDataString(
                blobUri.AbsolutePath.Split('/').Last());

            var blobClient = _containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync();

            return (
                response.Value.Content,
                response.Value.Details.ContentType
                    ?? "application/octet-stream"
            );
        }
    }
}
