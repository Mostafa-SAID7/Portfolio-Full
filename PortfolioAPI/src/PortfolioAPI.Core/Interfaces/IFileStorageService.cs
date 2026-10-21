using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// The storage abstraction every media write goes through.
    ///
    /// Two implementations exist: <c>FileStorageService</c> (the API server's
    /// own disk, App_Data/uploads) and <c>CloudinaryStorageService</c>
    /// (Cloudinary, delivered through the Cloudinary CDN). Controllers and
    /// services depend on this interface only - no provider detail (API keys,
    /// public ids, transformation syntax) ever leaks into them.
    ///
    /// The interface lives in Core (not next to its implementation) so the
    /// Application layer can use it too - deleting a project has to delete the
    /// media it owns, and that decision belongs to the service, not to a
    /// controller.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// The active provider: <see cref="MediaProvider.Local"/> or
        /// <see cref="MediaProvider.Cloudinary"/>.
        /// </summary>
        string Provider { get; }

        // ------------------------------------------------------------------
        // Provider-agnostic media operations - the primary surface.
        // ------------------------------------------------------------------

        /// <summary>
        /// Validates and stores an image, returning both its delivery URLs and
        /// the provider metadata needed to manage it later.
        /// </summary>
        Task<StoredImage> UploadImageAsync(ImageUploadRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a new image and removes the asset it replaces. The previous
        /// asset is only removed AFTER the new one was stored successfully, so
        /// a failed upload can never leave the caller with a broken image.
        /// </summary>
        Task<StoredImage> ReplaceImageAsync(ImageUploadRequest request, MediaReference previous, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a stored asset. Returns false when there was nothing to
        /// delete. Never throws for a missing asset - deletion is idempotent.
        /// </summary>
        Task<bool> DeleteImageAsync(MediaReference reference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the delivery URL for a stored asset. Falls back to the
        /// stored URL for providers that have no separate delivery form.
        /// </summary>
        string GetImageUrl(MediaReference reference);

        // ------------------------------------------------------------------
        // File-oriented surface (documents, and the legacy image helpers).
        // ------------------------------------------------------------------

        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileAsync(string fileUrl);
        Task DeleteFileAsync(string fileUrl);
        Task<bool> FileExistsAsync(string fileUrl);
        Task<long> GetFileSizeAsync(string fileUrl);
        Task<string> SaveImageAsync(Stream imageStream, string fileName, string? projectTitle = null);
        Task<(string Url640, string Url1280)> SaveOptimizedResponsiveImageAsync(Stream imageStream, string? projectSlug = null, string? oldImageUrl = null);
        Task<string> SaveDocumentAsync(Stream documentStream, string fileName);
    }

    /// <summary>
    /// Validates an uploaded image against the configured limits.
    ///
    /// Every provider runs uploads through this validator, so the rules are
    /// identical no matter where the bytes end up: extension, content type,
    /// magic bytes, size, and pixel dimensions.
    /// </summary>
    public interface IImageValidator
    {
        /// <summary>The limits currently in force.</summary>
        ImageValidationLimits Limits { get; }

        /// <summary>
        /// Reads and validates an image. Throws <see cref="InvalidOperationException"/>
        /// with a message that is safe to show to an admin when the upload is
        /// rejected.
        ///
        /// When <paramref name="fileName"/> is null/blank the caller only has the
        /// raw stream, so the format is decided by the magic bytes alone; when it
        /// is present the extension must be on the allowlist and the bytes must
        /// match it.
        /// </summary>
        Task<ValidatedImage> ValidateAsync(Stream content, string? fileName, string? contentType, CancellationToken cancellationToken = default);
    }
}
