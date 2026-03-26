namespace PATHFINDER_BACKEND.Services
{
    public class LocalFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _logoUploadPath;

        public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
            
            // Get base URL from configuration or use default
            _baseUrl = _configuration["Storage:BaseUrl"] ?? "http://localhost:5249";
            
            // Set logo upload path
            _logoUploadPath = Path.Combine(_environment.WebRootPath, "uploads", "company-logos");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_logoUploadPath))
            {
                Directory.CreateDirectory(_logoUploadPath);
            }
        }

        /// <summary>
        /// Uploads company logo to local file system
        /// </summary>
        /// <param name="file">The logo image file</param>
        /// <param name="companyId">The company ID</param>
        /// <returns>The URL of the uploaded logo</returns>
        public async Task<string> UploadCompanyLogoAsync(IFormFile file, int companyId)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid logo file type. Allowed: JPG, PNG, GIF, SVG, WEBP.");
            }
            
            // Validate file size (5MB max for logos)
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Logo file is too large. Max 5MB.");
            }

            // Create company-specific subdirectory
            var companyFolder = Path.Combine(_logoUploadPath, $"company-{companyId}");
            if (!Directory.Exists(companyFolder))
            {
                Directory.CreateDirectory(companyFolder);
            }

            // Generate unique filename
            var fileName = $"logo_{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine("uploads", "company-logos", $"company-{companyId}", fileName).Replace("\\", "/");
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the full URL
            return $"{_baseUrl}/{relativePath}";
        }

        /// <summary>
        /// Deletes company logo from local file system
        /// </summary>
        public async Task DeleteCompanyLogoAsync(int companyId, string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl))
                return;

            try
            {
                // Extract relative path from URL
                var uri = new Uri(logoUrl);
                var relativePath = uri.LocalPath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                
                // Delete the file if it exists
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                // Try to delete empty company folder
                var companyFolder = Path.GetDirectoryName(fullPath);
                if (companyFolder != null && Directory.Exists(companyFolder) && !Directory.EnumerateFileSystemEntries(companyFolder).Any())
                {
                    Directory.Delete(companyFolder);
                }
            }
            catch
            {
                // Log error but don't fail the operation
            }
            
            await Task.CompletedTask;
        }
    }
}