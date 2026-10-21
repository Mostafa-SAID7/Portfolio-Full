namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// Identity of the storage backend that owns a media asset.
    ///
    /// The value is persisted alongside the asset so a record can always be
    /// resolved back to the provider that created it - a Cloudinary delivery
    /// URL is NEVER used as the only identifier (it cannot be parsed back
    /// safely, and the URL format is a Cloudinary implementation detail).
    /// </summary>
    public static class MediaProvider
    {
        /// <summary>Files on the API server's own disk (App_Data/uploads).</summary>
        public const string Local = "local";

        /// <summary>Assets on Cloudinary, delivered through the Cloudinary CDN.</summary>
        public const string Cloudinary = "cloudinary";

        /// <summary>
        /// Normalizes a persisted/queried provider value. Anything unknown (and
        /// anything empty) is treated as <see cref="Local"/>, because every
        /// record written before Cloudinary existed is a local file.
        /// </summary>
        public static string Normalize(string? provider) =>
            string.Equals(provider, Cloudinary, StringComparison.OrdinalIgnoreCase)
                ? Cloudinary
                : Local;
    }

    /// <summary>
    /// A pointer to a stored asset: the delivery URL plus the provider-native
    /// identity (the Cloudinary public id). The public id is what makes
    /// delete / replace / rename possible without parsing a URL.
    /// </summary>
    public sealed record MediaReference(string? Url, string? PublicId, string? Provider)
    {
        /// <summary>An empty reference - nothing to delete, nothing to resolve.</summary>
        public static MediaReference None { get; } = new(null, null, null);

        /// <summary>The provider that owns this asset.</summary>
        public string ResolvedProvider => MediaProvider.Normalize(Provider);

        /// <summary>True when the asset lives on Cloudinary.</summary>
        public bool IsCloudinary => ResolvedProvider == MediaProvider.Cloudinary;

        /// <summary>True when there is nothing meaningful to act on.</summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Url) && string.IsNullOrWhiteSpace(PublicId);
    }

    /// <summary>
    /// The result of a successful image upload. Carries BOTH the delivery URLs
    /// (what the frontend consumes) and the provider metadata (what the
    /// backend needs to delete or replace the asset later).
    /// </summary>
    /// <param name="Provider">"local" or "cloudinary".</param>
    /// <param name="PublicId">Provider-native identity (Cloudinary public id). Null for local files.</param>
    /// <param name="Url">Primary delivery URL (the 1280px asset for local storage).</param>
    /// <param name="Url640">Delivery URL for the 640px slot.</param>
    /// <param name="Url1280">Delivery URL for the 1280px slot.</param>
    /// <param name="FileName">File name of the stored asset.</param>
    /// <param name="Bytes">Stored size in bytes.</param>
    /// <param name="Width">Source image width in pixels.</param>
    /// <param name="Height">Source image height in pixels.</param>
    public sealed record StoredImage(
        string Provider,
        string? PublicId,
        string Url,
        string Url640,
        string Url1280,
        string FileName,
        long Bytes,
        int Width,
        int Height);

    /// <summary>An upload request handed to a storage provider.</summary>
    public sealed class ImageUploadRequest
    {
        /// <summary>The raw image bytes.</summary>
        public required Stream Content { get; init; }

        /// <summary>The client-supplied file name (never used verbatim as a storage path).</summary>
        public required string FileName { get; init; }

        /// <summary>
        /// The owning project's slug. It scopes the provider-native asset id
        /// so assets stay grouped and predictable, and it is sanitized before
        /// it ever reaches a storage path.
        /// </summary>
        public string? Slug { get; init; }

        /// <summary>
        /// The kind of asset ("cover", "gallery", ...). It becomes a folder
        /// segment in the provider-native asset id.
        /// </summary>
        public string AssetType { get; init; } = "cover";
    }

    /// <summary>
    /// Hard limits applied to every uploaded image. They are configuration, not
    /// constants, so an operator can tighten them without a code change.
    /// </summary>
    public sealed class ImageValidationLimits
    {
        /// <summary>Maximum accepted upload size in bytes.</summary>
        public long MaxBytes { get; init; } = 10 * 1024 * 1024;

        /// <summary>Maximum accepted image width in pixels.</summary>
        public int MaxWidth { get; init; } = 8000;

        /// <summary>Maximum accepted image height in pixels.</summary>
        public int MaxHeight { get; init; } = 8000;

        /// <summary>
        /// Maximum accepted total pixel count. This is the decompression-bomb
        /// guard: a 20000x20000 PNG is only 40 MB on disk but expands to
        /// gigabytes in memory when decoded.
        /// </summary>
        public long MaxPixels { get; init; } = 40_000_000;
    }

    /// <summary>A validated image: its bytes plus the metadata derived from them.</summary>
    /// <param name="Content">The exact bytes that must be stored.</param>
    /// <param name="ContentType">The MIME type derived from the file signature.</param>
    /// <param name="Format">The detected format ("jpeg", "png", "gif", "webp").</param>
    /// <param name="Width">Image width in pixels.</param>
    /// <param name="Height">Image height in pixels.</param>
    public sealed record ValidatedImage(byte[] Content, string ContentType, string Format, int Width, int Height);
}
