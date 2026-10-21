using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// Shared location/URL rules for backend-owned uploads. Program.cs serves
    /// this folder at the public base URL and <see cref="FileStorageService"/>
    /// writes into it, so one configuration drives both sides.
    ///
    /// Storage lives under App_Data (never part of the publish output), so a
    /// redeploy of the API can never clobber uploaded files, and IIS never
    /// serves App_Data URLs directly — only our /api/files middleware does.
    /// </summary>
    public static class UploadStorage
    {
        public const string DefaultRelativePath = "App_Data/uploads";
        public const string DefaultBaseUrl = "/api/files";

        /// <summary>Absolute storage folder (created on demand).</summary>
        public static string ResolveRoot(IConfiguration configuration)
        {
            var configured = configuration["FileStorage:Path"];
            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath)
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(Directory.GetCurrentDirectory(), configured);
            Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>Public base path files are served under (default /api/files).</summary>
        public static string PublicBase(IConfiguration configuration)
        {
            var configured = configuration["FileStorage:BaseUrl"];
            return string.IsNullOrWhiteSpace(configured) ? DefaultBaseUrl : configured.TrimEnd('/');
        }

        /// <summary>
        /// True when <paramref name="url"/> points at a file this API owns
        /// (uploaded through UploadController) — external URLs (GitHub etc.)
        /// and portfolio-bundled paths must never be deleted by the API.
        /// </summary>
        public static bool IsManagedFile(string? url, IConfiguration configuration) =>
            !string.IsNullOrWhiteSpace(url) &&
            url.StartsWith(PublicBase(configuration) + "/", StringComparison.OrdinalIgnoreCase) &&
            !url.Contains("..", StringComparison.Ordinal);
    }

    public class FileStorageService : IFileStorageService
    {
        /// <summary>
        /// Extension → content type + magic-byte sniffers. Uploads are
        /// validated against the actual bytes, so a renamed executable or an
        /// SVG carrying scripts can never be served from the API origin.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, (string ContentType, Func<byte[], bool> Sniff)> AllowedImages =
            new Dictionary<string, (string, Func<byte[], bool>)>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = ("image/jpeg", IsJpeg),
                [".jpeg"] = ("image/jpeg", IsJpeg),
                [".png"] = ("image/png", IsPng),
                [".gif"] = ("image/gif", IsGif),
                [".webp"] = ("image/webp", IsWebp),
            };

        private const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string _storagePath;
        private readonly string _baseUrl;
        private readonly long _maxFileSize;

        public FileStorageService(IConfiguration configuration)
        {
            _storagePath = UploadStorage.ResolveRoot(configuration);
            _baseUrl = UploadStorage.PublicBase(configuration);

            var configuredMax = configuration["FileStorage:MaxFileSize"];
            _maxFileSize = long.TryParse(configuredMax, out var parsed) && parsed > 0
                ? parsed
                : DefaultMaxFileSize;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            ArgumentNullException.ThrowIfNull(fileStream);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedImages.TryGetValue(extension, out var allowed))
                throw new InvalidOperationException("Only JPG, PNG, GIF and WebP image files are allowed.");

            if (!string.Equals(allowed.ContentType, contentType?.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Content type '{contentType}' does not match the '{extension}' extension.");

            return await SaveValidatedAsync(fileStream, extension, allowed.Sniff);
        }

        public async Task<string> SaveImageAsync(Stream imageStream, string fileName, string? projectTitle = null)
        {
            ArgumentNullException.ThrowIfNull(imageStream);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase) ||
                !AllowedImages.TryGetValue(extension, out var allowed))
                throw new InvalidOperationException("Images must be converted to WebP before uploading. Select the image again in a supported browser.");

            var descriptiveName = string.IsNullOrWhiteSpace(projectTitle)
                ? Path.GetFileNameWithoutExtension(fileName)
                : projectTitle;
            var sourceBytes = await ReadCappedAsync(imageStream);
            if (sourceBytes.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");
            if (!allowed.Sniff(sourceBytes))
                throw new InvalidOperationException("The file content does not match its extension.");

            return await SaveBytesAsync(sourceBytes, ".webp", descriptiveName);
        }

        public async Task<string> SaveOptimizedImageAsync(Stream imageStream, string? projectSlug = null)
        {
            ArgumentNullException.ThrowIfNull(imageStream);

            var sourceBytes = await ReadCappedAsync(imageStream);
            if (sourceBytes.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");

            // Validate magic bytes against allowed image types
            var (contentType, sniff) = DetectImageType(sourceBytes);
            if (sniff is null)
                throw new InvalidOperationException("Only JPG, PNG, GIF and WebP image files are allowed.");
            if (!sniff(sourceBytes))
                throw new InvalidOperationException("The file content does not match its extension.");

            // Process image: resize if width > 1600px (maintain aspect ratio), convert to WebP quality 80
            using var image = Image.Load(sourceBytes);
            if (image.Width > 1600)
            {
                var scale = 1600.0 / image.Width;
                var newHeight = (int)Math.Round(image.Height * scale);
                image.Mutate(x => x.Resize(1600, newHeight));
            }

            using var outputStream = new MemoryStream();
            var encoder = new WebpEncoder { Quality = 80 };
            await image.SaveAsync(outputStream, encoder);
            var optimizedBytes = outputStream.ToArray();

            // Sanitize slug for file name
            var safeSlug = SanitizeSlug(projectSlug);
            var uniqueFileName = GetUniqueFileName(safeSlug);

            var filePath = Path.Combine(_storagePath, uniqueFileName);
            await File.WriteAllBytesAsync(filePath, optimizedBytes);
            return $"{_baseUrl}/{uniqueFileName}";
        }

        private static (string ContentType, Func<byte[], bool>? Sniff) DetectImageType(byte[] bytes)
        {
            if (IsJpeg(bytes)) return ("image/jpeg", IsJpeg);
            if (IsPng(bytes)) return ("image/png", IsPng);
            if (IsGif(bytes)) return ("image/gif", IsGif);
            if (IsWebp(bytes)) return ("image/webp", IsWebp);
            return (string.Empty, null);
        }

        private static string SanitizeSlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return "image";

            // Normalize to FormD, lowercase, replace non-alphanumeric with -
            var normalized = slug.Normalize(System.Text.NormalizationForm.FormD).ToLowerInvariant();
            var slugified = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slugified) ? "image" : slugified;
        }

        private string GetUniqueFileName(string baseSlug)
        {
            var fileName = $"{baseSlug}.webp";
            var filePath = Path.Combine(_storagePath, fileName);
            if (!File.Exists(filePath))
                return fileName;

            var counter = 2;
            while (true)
            {
                fileName = $"{baseSlug}-{counter}.webp";
                filePath = Path.Combine(_storagePath, fileName);
                if (!File.Exists(filePath))
                    return fileName;
                counter++;
            }
        }

        public async Task<string> SaveDocumentAsync(Stream documentStream, string fileName)
        {
            ArgumentNullException.ThrowIfNull(documentStream);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            Func<byte[], bool> sniff = extension switch
            {
                ".pdf" => bytes => bytes.Length > 4 && bytes[0] == '%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' && bytes[3] == (byte)'F',
                ".doc" or ".docx" => bytes => bytes.Length > 3 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K',
                ".txt" or ".csv" => _ => true,
                _ => throw new InvalidOperationException("Only PDF, Word, TXT and CSV documents are allowed.")
            };

            return await SaveValidatedAsync(documentStream, extension, sniff);
        }

        /// <summary>Buffers with a hard cap, validates bytes, and writes the file with a safe unique name.</summary>
        private async Task<string> SaveValidatedAsync(Stream fileStream, string extension, Func<byte[], bool> sniff, string? preferredName = null)
        {
            var bytes = await ReadCappedAsync(fileStream);
            if (bytes.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");
            if (!sniff(bytes))
                throw new InvalidOperationException("The file content does not match its extension.");

            return await SaveBytesAsync(bytes, extension, preferredName);
        }

        private async Task<string> SaveBytesAsync(byte[] bytes, string extension, string? preferredName)
        {
            var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..16].ToLowerInvariant();
            var uniqueFileName = string.IsNullOrWhiteSpace(preferredName)
                ? $"{Guid.NewGuid()}{extension}"
                : $"{ToSafeFileName(preferredName)}-{contentHash}{extension}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);
            if (!File.Exists(filePath))
                await File.WriteAllBytesAsync(filePath, bytes);
            return $"{_baseUrl}/{uniqueFileName}";
        }

        private static string ToSafeFileName(string value)
        {
            var slug = System.Text.RegularExpressions.Regex.Replace(
                value.Normalize(System.Text.NormalizationForm.FormD).ToLowerInvariant(),
                @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "project-image" : slug[..Math.Min(slug.Length, 64)];
        }

        private async Task<byte[]> ReadCappedAsync(Stream fileStream)
        {
            await using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            long total = 0;
            int read;
            while ((read = await fileStream.ReadAsync(chunk.AsMemory(0, chunk.Length))) > 0)
            {
                total += read;
                if (total > _maxFileSize)
                    throw new InvalidOperationException(
                        $"File size exceeds the maximum allowed size of {_maxFileSize / 1024 / 1024}MB.");
                await buffer.WriteAsync(chunk.AsMemory(0, read));
            }
            return buffer.ToArray();
        }

        public async Task<Stream> GetFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty.", nameof(fileUrl));

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return await Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty.", nameof(fileUrl));

            // Path.GetFileName strips any directory components — a traversal
            // attempt like "../../web.config" can never escape the store.
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return Task.FromResult(false);

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);
            return Task.FromResult(File.Exists(filePath));
        }

        public Task<long> GetFileSizeAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty.", nameof(fileUrl));

            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return Task.FromResult(new FileInfo(filePath).Length);
        }

        private static bool IsJpeg(byte[] b) =>
            b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

        private static bool IsPng(byte[] b) =>
            b.Length > 8 &&
            b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
            b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A;

        private static bool IsGif(byte[] b) =>
            b.Length > 5 &&
            b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8' &&
            (b[4] == '7' || b[4] == '9') && b[5] == 'a';

        private static bool IsWebp(byte[] b) =>
            b.Length > 12 &&
            b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' &&
            b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P';

    }

    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileAsync(string fileUrl);
        Task DeleteFileAsync(string fileUrl);
        Task<bool> FileExistsAsync(string fileUrl);
        Task<long> GetFileSizeAsync(string fileUrl);
        Task<string> SaveImageAsync(Stream imageStream, string fileName, string? projectTitle = null);
        Task<string> SaveOptimizedImageAsync(Stream imageStream, string? projectSlug = null);
        Task<string> SaveDocumentAsync(Stream documentStream, string fileName);
    }
}
