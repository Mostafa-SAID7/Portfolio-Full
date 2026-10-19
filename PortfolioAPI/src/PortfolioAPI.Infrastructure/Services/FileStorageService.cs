using Microsoft.Extensions.Configuration;

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
                [".avif"] = ("image/avif", IsAvif),
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
                throw new InvalidOperationException("Only JPG, PNG, GIF, WebP and AVIF image files are allowed.");

            if (!string.Equals(allowed.ContentType, contentType?.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Content type '{contentType}' does not match the '{extension}' extension.");

            return await SaveValidatedAsync(fileStream, extension, allowed.Sniff);
        }

        public async Task<string> SaveImageAsync(Stream imageStream, string fileName)
        {
            ArgumentNullException.ThrowIfNull(imageStream);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedImages.TryGetValue(extension, out var allowed))
                throw new InvalidOperationException("Only JPG, PNG, GIF, WebP and AVIF image files are allowed.");

            return await SaveValidatedAsync(imageStream, extension, allowed.Sniff);
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

        /// <summary>Buffers with a hard cap, sniffs the bytes, then writes with a random name.</summary>
        private async Task<string> SaveValidatedAsync(Stream fileStream, string extension, Func<byte[], bool> sniff)
        {
            var bytes = await ReadCappedAsync(fileStream);
            if (bytes.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");
            if (!sniff(bytes))
                throw new InvalidOperationException("The file content does not match its extension.");

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);
            await File.WriteAllBytesAsync(filePath, bytes);
            return $"{_baseUrl}/{uniqueFileName}";
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

        private static bool IsAvif(byte[] b)
        {
            // ISO-BMFF: [4..8] == "ftyp", major brand or compatible brands contain "avif"/"avis".
            if (b.Length < 16 || !(b[4] == 'f' && b[5] == 't' && b[6] == 'y' && b[7] == 'p'))
                return false;
            for (var offset = 8; offset + 4 <= Math.Min(b.Length, 32); offset += 4)
            {
                if ((b[offset] == 'a' && b[offset + 1] == 'v' && b[offset + 2] == 'i' && b[offset + 3] == 'f') ||
                    (b[offset] == 'a' && b[offset + 1] == 'v' && b[offset + 2] == 'i' && b[offset + 3] == 's'))
                    return true;
            }
            return false;
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
