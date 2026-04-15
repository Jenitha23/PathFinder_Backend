using Azure.Storage.Blobs;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace PATHFINDER_BACKEND.Services
{
    /// <summary>
    /// Service for extracting text from CV files (PDF, DOCX) stored in Azure Blob Storage
    /// </summary>
    public class CvTextExtractorService
    {
        private readonly ILogger<CvTextExtractorService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly string _blobConnectionString;
        private readonly string _blobContainerName;
        private readonly string _tempDirectory;
        private readonly bool _useBlobStorage;

        public CvTextExtractorService(
            IConfiguration configuration, 
            ILogger<CvTextExtractorService> logger,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            
            // Setup temp directory for downloading blobs
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PathFinder_CV_Extracts");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }

            // Check if using Azure Blob Storage or local storage
            _blobConnectionString = configuration["AzureBlobStorage:ConnectionString"] ?? "";
            _blobContainerName = configuration["AzureBlobStorage:ContainerName"] ?? "student-cvs";
            _useBlobStorage = !string.IsNullOrEmpty(_blobConnectionString);
            
            _logger.LogInformation($"CvTextExtractorService initialized. Using Blob Storage: {_useBlobStorage}");
        }

        /// <summary>
        /// Extract text from CV file (supports PDF, DOCX)
        /// </summary>
        /// <param name="fileUrl">URL of the CV file (Azure Blob URL or local path)</param>
        /// <returns>Extracted text content</returns>
        public async Task<string> ExtractTextFromCvAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                _logger.LogWarning("Empty CV URL provided");
                return "";
            }

            string? localFilePath = null;
            
            try
            {
                // Download file from blob storage or get local path
                localFilePath = await DownloadFileToLocalAsync(fileUrl);
                
                if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                {
                    _logger.LogError($"File not found after download: {localFilePath}");
                    return "";
                }

                // Extract text based on file extension
                var extension = Path.GetExtension(localFilePath).ToLowerInvariant();
                string extractedText = extension switch
                {
                    ".pdf" => ExtractTextFromPdf(localFilePath),
                    ".docx" => ExtractTextFromDocx(localFilePath),
                    ".doc" => await ExtractTextFromDocAsync(localFilePath),
                    _ => throw new NotSupportedException($"Unsupported file type: {extension}. Supported types: PDF, DOCX, DOC")
                };

                // Clean and normalize extracted text
                extractedText = CleanExtractedText(extractedText);
                
                _logger.LogInformation($"Successfully extracted {extractedText.Length} characters from CV: {Path.GetFileName(fileUrl)}");
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to extract text from CV: {fileUrl}");
                throw;
            }
            finally
            {
                // Clean up temporary file
                if (!string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
                {
                    try
                    {
                        File.Delete(localFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete temp file: {localFilePath}");
                    }
                }
            }
        }

        /// <summary>
        /// Extract text from PDF using iText7
        /// </summary>
        private string ExtractTextFromPdf(string filePath)
        {
            var text = new System.Text.StringBuilder();
            
            try
            {
                using (var pdfReader = new PdfReader(filePath))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    int pageCount = pdfDoc.GetNumberOfPages();
                    for (int page = 1; page <= pageCount; page++)
                    {
                        var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                        if (!string.IsNullOrEmpty(pageText))
                        {
                            text.AppendLine(pageText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from PDF: {filePath}");
                throw;
            }
            
            return text.ToString();
        }

        /// <summary>
        /// Extract text from DOCX using OpenXML
        /// </summary>
        private string ExtractTextFromDocx(string filePath)
        {
            var text = new System.Text.StringBuilder();
            
            try
            {
                using (var wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    // Safe navigation with null checks
                    var mainPart = wordDoc.MainDocumentPart;
                    if (mainPart != null)
                    {
                        var document = mainPart.Document;
                        if (document != null)
                        {
                            var body = document.Body;
                            if (body != null)
                            {
                                foreach (var paragraph in body.Elements<Paragraph>())
                                {
                                    foreach (var run in paragraph.Elements<Run>())
                                    {
                                        foreach (var textElement in run.Elements<Text>())
                                        {
                                            if (textElement.Text != null)
                                            {
                                                text.Append(textElement.Text);
                                            }
                                        }
                                    }
                                    text.AppendLine();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from DOCX: {filePath}");
                throw;
            }
            
            return text.ToString();
        }

        /// <summary>
        /// Extract text from legacy DOC files using alternative method
        /// </summary>
        private async Task<string> ExtractTextFromDocAsync(string filePath)
        {
            _logger.LogWarning("Legacy .doc files are not fully supported. Consider converting to .docx");
            
            try
            {
                // Fallback: Try to read as text (may not work well)
                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from DOC file: {filePath}");
                return ""; // Return empty string on failure
            }
        }

        /// <summary>
        /// Download file from Azure Blob Storage or get local path
        /// </summary>
        private async Task<string> DownloadFileToLocalAsync(string fileUrl)
        {
            // Check if it's a local file path (not a URL)
            if (!fileUrl.StartsWith("http://") && !fileUrl.StartsWith("https://"))
            {
                // Local file path - resolve from wwwroot
                var localPath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
                if (File.Exists(localPath))
                    return localPath;
                    
                _logger.LogWarning($"Local file not found: {localPath}");
                return fileUrl;
            }

            // Azure Blob Storage URL
            var fileName = GenerateSafeFileName(fileUrl);
            var tempFilePath = Path.Combine(_tempDirectory, fileName);

            if (_useBlobStorage)
            {
                try
                {
                    // Validate connection string
                    if (string.IsNullOrEmpty(_blobConnectionString))
                    {
                        throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
                    }

                    // Parse blob URL to get blob name
                    var blobUri = new Uri(fileUrl);
                    var blobName = blobUri.Segments.LastOrDefault();
                    
                    if (string.IsNullOrEmpty(blobName))
                    {
                        blobName = fileName;
                    }

                    var blobContainerClient = new BlobContainerClient(_blobConnectionString, _blobContainerName);
                    var blobClient = blobContainerClient.GetBlobClient(blobName);
                    
                    // Download blob to temp file
                    await blobClient.DownloadToAsync(tempFilePath);
                    _logger.LogInformation($"Downloaded blob to temp file: {tempFilePath}");
                    
                    return tempFilePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to download blob from Azure: {fileUrl}");
                    throw new Exception($"Failed to download CV from storage: {ex.Message}");
                }
            }
            else
            {
                // Fallback: Try to download via HTTP client
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    var response = await httpClient.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();
                    
                    using var fileStream = File.Create(tempFilePath);
                    await response.Content.CopyToAsync(fileStream);
                    
                    _logger.LogInformation($"Downloaded via HTTP to temp file: {tempFilePath}");
                    return tempFilePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to download via HTTP: {fileUrl}");
                    throw new Exception($"Failed to download CV via HTTP: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Generate safe filename from URL
        /// </summary>
        private string GenerateSafeFileName(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                
                if (string.IsNullOrEmpty(fileName))
                    fileName = Guid.NewGuid().ToString();
                    
                // Remove any invalid characters from filename
                fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9_.-]", "_");
                
                // Add timestamp to avoid collisions
                var timestamp = DateTime.Now.Ticks;
                var extension = Path.GetExtension(fileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                
                return $"{nameWithoutExt}_{timestamp}{extension}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to parse URL, using GUID filename: {url}");
                return $"{Guid.NewGuid()}_{DateTime.Now.Ticks}.pdf";
            }
        }

        /// <summary>
        /// Clean extracted text by removing extra whitespace and normalizing
        /// </summary>
        private string CleanExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
                
            try
            {
                // Replace multiple newlines with single newline
                text = Regex.Replace(text, @"\r\n\s*\r\n", "\n\n");
                text = Regex.Replace(text, @"\n{3,}", "\n\n");
                
                // Replace multiple spaces with single space
                text = Regex.Replace(text, @"[ ]{2,}", " ");
                
                // Remove any null characters
                text = text.Replace("\0", "");
                
                // Trim
                text = text.Trim();
                
                // Limit length to reasonable size (20KB for AI processing)
                if (text.Length > 20000)
                {
                    text = text.Substring(0, 20000);
                    _logger.LogWarning($"CV text truncated to 20000 characters from original {text.Length}");
                }
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning extracted text");
                return text; // Return original on error
            }
        }
    }
}