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

        /// <summary>
        /// Upload student CV to Azure Blob Storage
        /// </summary>
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

        /// <summary>
        /// Uploads company logo to Azure Blob Storage
        /// </summary>
        public async Task<string> UploadCompanyLogoAsync(IFormFile file, int companyId)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Invalid logo file type. Allowed: JPG, PNG, GIF, SVG, WEBP."
                );
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Logo file is too large. Max 5MB.");
            }

            var containerClient = new BlobContainerClient(_connectionString, "company-logos");
            await containerClient.CreateIfNotExistsAsync();

            var fileName = $"company-{companyId}/logo_{Guid.NewGuid()}{extension}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Deletes company logo from Azure Blob Storage
        /// </summary>
        public async Task DeleteCompanyLogoAsync(int companyId, string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return;

            try
            {
                var uri = new Uri(logoUrl);
                var blobName = uri.Segments.LastOrDefault() ?? "";

                if (!string.IsNullOrWhiteSpace(blobName))
                {
                    var containerClient = new BlobContainerClient(_connectionString, "company-logos");
                    var blobClient = containerClient.GetBlobClient($"company-{companyId}/{blobName}");

                    await blobClient.DeleteIfExistsAsync();
                }
            }
            catch
            {
                // Log error but don't fail
            }
        }
    }
}