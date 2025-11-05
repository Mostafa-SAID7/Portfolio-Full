using Microsoft.Extensions.Configuration;

namespace PortfolioAPI.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _storagePath;
        private readonly string _baseUrl;

        public FileStorageService(IConfiguration configuration)
        {
            _storagePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads";

            // Ensure storage directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            // Generate unique filename to prevent conflicts
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);

            // Validate file type (basic security check)
            if (!IsValidFileType(contentType))
            {
                throw new InvalidOperationException("Invalid file type. Only images and documents are allowed.");
            }

            // Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (fileStream.Length > maxFileSize)
            {
                throw new InvalidOperationException("File size exceeds the maximum allowed size of 10MB.");
            }

            using (var fileStreamToWrite = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamToWrite);
            }

            // Return the relative URL
            return $"{_baseUrl}/{uniqueFileName}";
        }

        public async Task<Stream> GetFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty", nameof(fileUrl));

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty", nameof(fileUrl));

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async Task<bool> FileExistsAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return false;

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            return File.Exists(filePath);
        }

        public async Task<long> GetFileSizeAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty", nameof(fileUrl));

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        public async Task<string> SaveImageAsync(Stream imageStream, string fileName)
        {
            // Validate that it's an image
            var contentType = GetContentTypeFromFileName(fileName);
            if (!contentType.StartsWith("image/"))
            {
                throw new InvalidOperationException("Only image files are allowed for this operation.");
            }

            return await SaveFileAsync(imageStream, fileName, contentType);
        }

        public async Task<string> SaveDocumentAsync(Stream documentStream, string fileName)
        {
            // Validate that it's a document
            var contentType = GetContentTypeFromFileName(fileName);
            var allowedTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

            if (!allowedTypes.Contains(contentType))
            {
                throw new InvalidOperationException("Only PDF and Word documents are allowed for this operation.");
            }

            return await SaveFileAsync(documentStream, fileName, contentType);
        }

        private bool IsValidFileType(string contentType)
        {
            var allowedTypes = new[]
            {
                // Images
                "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
                // Documents
                "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain", "text/csv"
            };

            return allowedTypes.Contains(contentType.ToLower());
        }

        private string GetContentTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }

    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileAsync(string fileUrl);
        Task DeleteFileAsync(string fileUrl);
        Task<bool> FileExistsAsync(string fileUrl);
        Task<long> GetFileSizeAsync(string fileUrl);
        Task<string> SaveImageAsync(Stream imageStream, string fileName);
        Task<string> SaveDocumentAsync(Stream documentStream, string fileName);
    }
}