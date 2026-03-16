using Azure.Storage.Blobs;

namespace PATHFINDER_BACKEND.Services
{
    public class BlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobService(IConfiguration configuration)
        {
            _connectionString = configuration["BlobConnectionString"]
                ?? throw new Exception("BlobConnectionString is missing.");

            _containerName = configuration["BlobContainerName"]
                ?? throw new Exception("BlobContainerName is missing.");
        }

        public async Task<string> UploadCvAsync(IFormFile file, int studentId)
        {
            var containerClient = new BlobContainerClient(_connectionString, _containerName);
            await containerClient.CreateIfNotExistsAsync();

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"student-{studentId}/cv_{Guid.NewGuid()}{ext}";

            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }
    }
}