using PortfolioAPI.Core.Interfaces;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// Local-disk upload settings (<c>FileStorage</c> section).
    ///
    /// Bound once at startup and shared by the file server in Program.cs, the
    /// local storage provider and the image validator, so one section drives
    /// where files go, how they are served and what is accepted.
    /// </summary>
    public sealed class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        /// <summary>Upload folder (relative to the content root, or absolute).</summary>
        public string Path { get; set; } = UploadStorage.DefaultRelativePath;

        /// <summary>Public path local uploads are served under.</summary>
        public string BaseUrl { get; set; } = UploadStorage.DefaultBaseUrl;

        /// <summary>Hard cap on a single upload, in bytes.</summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

        /// <summary>Maximum accepted image width in pixels.</summary>
        public int MaxImageWidth { get; set; } = 8000;

        /// <summary>Maximum accepted image height in pixels.</summary>
        public int MaxImageHeight { get; set; } = 8000;

        /// <summary>Maximum accepted total pixel count (decompression-bomb guard).</summary>
        public long MaxImagePixels { get; set; } = 40_000_000;

        /// <summary>Turns the stored settings into the limits the validator enforces.</summary>
        public ImageValidationLimits ToLimits() => new()
        {
            MaxBytes = MaxFileSize,
            MaxWidth = MaxImageWidth,
            MaxHeight = MaxImageHeight,
            MaxPixels = MaxImagePixels
        };
    }

    /// <summary>Cloudinary credentials (<c>Cloudinary</c> section).</summary>
    public sealed class CloudinaryOptions
    {
        public const string SectionName = "Cloudinary";

        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// Root folder every portfolio asset is stored under, e.g. "portfolio".
        /// Public ids always start with this segment, so assets stay grouped in
        /// the Cloudinary console and a media query can never touch anything
        /// outside the portfolio's own folder.
        /// </summary>
        public string Folder { get; set; } = "portfolio";

        /// <summary>
        /// True when every credential needed to reach Cloudinary is present.
        /// The VALUES are never read outside this class and are never logged.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(CloudName) &&
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(ApiSecret);
    }

    /// <summary>Active storage backend (<c>Storage</c> section).</summary>
    public sealed class StorageOptions
    {
        public const string SectionName = "Storage";

        /// <summary>
        /// "cloudinary" (production) or "local" (development / fallback).
        /// Anything else is treated as "local".
        /// </summary>
        public string Provider { get; set; } = MediaProvider.Local;
    }
}
