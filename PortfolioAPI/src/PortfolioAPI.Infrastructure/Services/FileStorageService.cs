using Microsoft.Extensions.Configuration;
using PortfolioAPI.Core.Interfaces;
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

    /// <summary>
    /// Local-disk implementation of <see cref="IFileStorageService"/>.
    ///
    /// Each cover becomes two physical WebP files (640px and 1280px) named after
    /// the project slug, and is served back from the API origin at a relative
    /// <c>/api/files/...</c> path.
    ///
    /// The Cloudinary provider is the production default; this one remains the
    /// development/fallback provider and the source of the local files that the
    /// media migration moves to Cloudinary.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private const string FallbackSlug = "project";

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

        private readonly string _storagePath;
        private readonly string _baseUrl;
        private readonly long _maxFileSize;
        private readonly IImageValidator _validator;

        /// <param name="validator">
        /// Injected by DI; tests may omit it and get one built from the same
        /// configuration, so the validation rules never diverge between paths.
        /// </param>
        public FileStorageService(IConfiguration configuration, IImageValidator? validator = null)
        {
            _storagePath = UploadStorage.ResolveRoot(configuration);
            _baseUrl = UploadStorage.PublicBase(configuration);
            _validator = validator ?? new ImageValidator(configuration);
            _maxFileSize = _validator.Limits.MaxBytes;
        }

        public string Provider => MediaProvider.Local;

        // ------------------------------------------------------------------
        // Provider-agnostic media operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Validates, then writes a 640px and a 1280px WebP of the image named
        /// after the project slug. No previous asset is removed here: the
        /// caller owns the database commit, so it deletes the old cover only
        /// once the new one is safely referenced.
        /// </summary>
        public async Task<StoredImage> UploadImageAsync(ImageUploadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Content);

            var validated = await _validator.ValidateAsync(request.Content, request.FileName, null, cancellationToken);

            var safeSlug = SanitizeSegment(request.Slug, FallbackSlug);
            using var image = Image.Load(validated.Content);

            var (fileName640, fileName1280) = GetUniqueResponsiveFileNames(safeSlug);
            var bytes640 = await WriteVariantAsync(image, fileName640, 640, cancellationToken);
            var bytes1280 = await WriteVariantAsync(image, fileName1280, 1280, cancellationToken);

            var url640 = $"{_baseUrl}/{fileName640}";
            var url1280 = $"{_baseUrl}/{fileName1280}";

            return new StoredImage(
                MediaProvider.Local,
                null,
                url1280,
                url640,
                url1280,
                fileName1280,
                bytes1280,
                image.Width,
                image.Height);
        }

        public async Task<StoredImage> ReplaceImageAsync(
            ImageUploadRequest request,
            MediaReference previous,
            CancellationToken cancellationToken = default)
        {
            // Upload first — the previous asset is only removed once the new
            // one is on disk, so a failed upload leaves the old cover intact.
            var stored = await UploadImageAsync(request, cancellationToken);

            if (previous.ResolvedProvider == MediaProvider.Local)
            {
                await DeleteImageAsync(previous, cancellationToken);
            }

            return stored;
        }

        /// <summary>
        /// Removes the file behind the URL and the responsive sibling that was
        /// written with it (they always share a counter). Deletion is
        /// idempotent: a file that is already gone is not an error.
        /// </summary>
        public Task<bool> DeleteImageAsync(MediaReference reference, CancellationToken cancellationToken = default)
        {
            if (reference.IsEmpty || reference.ResolvedProvider != MediaProvider.Local || reference.Url is null)
                return Task.FromResult(false);

            LocalUploadFiles.TryDeleteStoredImage(reference.Url, _storagePath);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Resolves a stored reference to the URL the browser should call. A
        /// reference that only carries a file name is still usable.
        /// </summary>
        public string GetImageUrl(MediaReference reference)
        {
            if (!string.IsNullOrWhiteSpace(reference.Url))
                return reference.Url!;

            if (string.IsNullOrWhiteSpace(reference.PublicId))
                return string.Empty;

            var fileName = Path.GetFileName(reference.PublicId);
            return string.IsNullOrWhiteSpace(fileName) ? string.Empty : $"{_baseUrl}/{fileName}";
        }

        // ------------------------------------------------------------------
        // Legacy file-oriented surface (kept for compatibility)
        // ------------------------------------------------------------------

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

        public async Task<(string Url640, string Url1280)> SaveOptimizedResponsiveImageAsync(
            Stream imageStream,
            string? projectSlug = null,
            string? oldImageUrl = null)
        {
            ArgumentNullException.ThrowIfNull(imageStream);

            // The same validator the Cloudinary path runs, so dimension and
            // pixel limits apply no matter where the bytes end up.
            var validated = await _validator.ValidateAsync(imageStream, fileName: null, contentType: null);

            // Legacy contract: the old pair is removed BEFORE the new files are
            // written, so the new upload can reuse the base names.
            if (!string.IsNullOrWhiteSpace(oldImageUrl))
                LocalUploadFiles.TryDeleteStoredImage(oldImageUrl, _storagePath);

            using var image = Image.Load(validated.Content);

            var (fileName640, fileName1280) = GetUniqueResponsiveFileNames(SanitizeSegment(projectSlug, FallbackSlug));
            await WriteVariantAsync(image, fileName640, 640);
            await WriteVariantAsync(image, fileName1280, 1280);

            return ($"{_baseUrl}/{fileName640}", $"{_baseUrl}/{fileName1280}");
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

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>
        /// Encodes one responsive variant and returns its byte length. The
        /// source is only resized when it is actually wider than the target, so
        /// a small upload is never upscaled.
        /// </summary>
        private async Task<long> WriteVariantAsync(Image source, string fileName, int size, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_storagePath, fileName);

            using var output = new MemoryStream();
            using (var resized = source.Clone(x =>
            {
                if (source.Width > size)
                {
                    var scale = (double)size / source.Width;
                    x.Resize(size, (int)Math.Round(source.Height * scale));
                }
            }))
            {
                await resized.SaveAsync(output, new WebpEncoder { Quality = 80 }, cancellationToken);
            }

            var bytes = output.ToArray();
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
            return bytes.LongLength;
        }

        /// <summary>
        /// Picks the 640/1280 file names as a pair so both carry the SAME
        /// counter. That is what makes "delete the old pair" exact: the files
        /// written by one upload are always <c>{slug}-640[-n].webp</c> and
        /// <c>{slug}-1280[-n].webp</c> with the same n, so removing one never
        /// removes a newer replacement.
        /// </summary>
        private (string FileName640, string FileName1280) GetUniqueResponsiveFileNames(string baseSlug)
        {
            for (var counter = 1; counter < int.MaxValue; counter++)
            {
                var suffix = counter == 1 ? string.Empty : $"-{counter}";
                var name640 = $"{baseSlug}-640{suffix}.webp";
                var name1280 = $"{baseSlug}-1280{suffix}.webp";

                if (!File.Exists(Path.Combine(_storagePath, name640)) &&
                    !File.Exists(Path.Combine(_storagePath, name1280)))
                    return (name640, name1280);
            }

            throw new InvalidOperationException("Could not find a free file name for the uploaded image.");
        }

        private static (string ContentType, Func<byte[], bool>? Sniff) DetectImageType(byte[] bytes)
        {
            if (IsJpeg(bytes)) return ("image/jpeg", IsJpeg);
            if (IsPng(bytes)) return ("image/png", IsPng);
            if (IsGif(bytes)) return ("image/gif", IsGif);
            if (IsWebp(bytes)) return ("image/webp", IsWebp);
            return (string.Empty, null);
        }

        /// <summary>
        /// Reduces any user-supplied text to <c>a-z0-9-</c>. An empty result
        /// becomes <paramref name="fallback"/>, so a title in a non-Latin script
        /// still produces a usable, path-safe name instead of an empty one.
        /// </summary>
        private static string SanitizeSegment(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var normalized = value.Normalize(System.Text.NormalizationForm.FormD).ToLowerInvariant();
            var sanitized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(sanitized))
                return fallback;

            return sanitized.Length > 100 ? sanitized[..100].TrimEnd('-') : sanitized;
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
}
