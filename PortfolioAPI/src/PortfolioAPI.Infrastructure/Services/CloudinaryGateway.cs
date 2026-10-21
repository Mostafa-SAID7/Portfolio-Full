using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// Raw result of a Cloudinary upload, reduced to exactly what the storage
    /// service needs. Nothing here ever carries credentials.
    /// </summary>
    public sealed record CloudinaryUploadOutcome(
        bool Success,
        string? PublicId,
        string? SecureUrl,
        string? Version,
        string? Format,
        long Bytes,
        int Width,
        int Height,
        string? Error);

    /// <summary>Stored metadata of an asset that already lives on Cloudinary.</summary>
    public sealed record CloudinaryAssetInfo(string PublicId, string? SecureUrl, long Bytes, int Width, int Height);

    /// <summary>
    /// Thin seam over the Cloudinary SDK.
    ///
    /// <c>CloudinaryStorageService</c> is fully unit-tested, so every SDK call
    /// goes through this interface and can be faked. The SDK itself is still
    /// the only thing that talks to Cloudinary — no hand-rolled HTTP, no
    /// hand-rolled signatures.
    /// </summary>
    public interface ICloudinaryGateway
    {
        /// <summary>Uploads an image. <paramref name="overwrite"/> is only ever true for a deliberate replace.</summary>
        Task<CloudinaryUploadOutcome> UploadAsync(Stream content, string publicId, bool overwrite, CancellationToken cancellationToken = default);

        /// <summary>Deletes an asset by its public id. Returns false when the asset was not there (deletion is idempotent).</summary>
        Task<bool> DeleteAsync(string publicId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads an asset's stored metadata, or null when nothing with this
        /// public id exists. This is what makes the upload id reservation and
        /// the media migration verification possible without a URL round-trip.
        /// </summary>
        Task<CloudinaryAssetInfo?> GetAsync(string publicId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// <see cref="ICloudinaryGateway"/> over the official CloudinaryDotNet SDK.
    /// The client is signed with the account credentials and always talks over
    /// HTTPS; the credentials never leave this class.
    /// </summary>
    public sealed class CloudinaryGateway : ICloudinaryGateway
    {
        /// <summary>Upload/delete results that are safe to show back to the admin.</summary>
        private const string GenericUploadFailure = "The image could not be uploaded to the media store.";
        private const string GenericDeleteFailure = "The stored image could not be removed from the media store.";

        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryGateway> _logger;

        public CloudinaryGateway(CloudinaryOptions options, ILogger<CloudinaryGateway> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            _logger = logger;

            // Fail fast, and never echo the values: the message only says WHICH
            // section is unusable.
            if (!options.IsConfigured)
                throw new InvalidOperationException("Cloudinary configuration is incomplete.");

            _cloudinary = new Cloudinary(new Account(
                options.CloudName.Trim(),
                options.ApiKey.Trim(),
                options.ApiSecret.Trim()));
        }

        public async Task<CloudinaryUploadOutcome> UploadAsync(
            Stream content,
            string publicId,
            bool overwrite,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("Public id cannot be null or empty.", nameof(publicId));

            try
            {
                var parameters = new ImageUploadParams
                {
                    // Only the file name Cloudinary shows in its console - the
                    // public id is ours, never derived from user input verbatim.
                    File = new FileDescription("cover", content),
                    PublicId = publicId,
                    Overwrite = overwrite,
                    UniqueFilename = false,
                    UseFilename = false,
                    // Busts Cloudinary's CDN cache when an asset is replaced.
                    Invalidate = true,
                    Type = "upload"
                };

                var result = await _cloudinary.UploadAsync(parameters, cancellationToken);

                if (result.StatusCode != HttpStatusCode.OK || result.Error is not null)
                {
                    _logger.LogWarning(
                        "Cloudinary rejected the upload for public id {PublicId}: status {StatusCode}, reason {Reason}",
                        publicId,
                        (int)result.StatusCode,
                        result.Error?.Message ?? "no reason given");

                    return new CloudinaryUploadOutcome(
                        false, null, null, null, null, 0, 0, 0,
                        $"The media store rejected the upload ({(int)result.StatusCode}).");
                }

                var secureUrl = result.SecureUrl?.ToString();
                if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(result.PublicId))
                {
                    _logger.LogWarning(
                        "Cloudinary upload for {PublicId} returned no secure URL or public id (status {StatusCode})",
                        publicId,
                        (int)result.StatusCode);

                    return new CloudinaryUploadOutcome(
                        false, null, null, null, null, 0, 0, 0,
                        "The media store returned an incomplete upload response.");
                }

                return new CloudinaryUploadOutcome(
                    true,
                    result.PublicId,
                    secureUrl!,
                    result.Version,
                    result.Format,
                    result.Bytes,
                    result.Width,
                    result.Height,
                    null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Cloudinary upload failed for public id {PublicId}", publicId);
                return new CloudinaryUploadOutcome(false, null, null, null, null, 0, 0, 0, GenericUploadFailure);
            }
        }

        public async Task<bool> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            try
            {
                var parameters = new DeletionParams(publicId)
                {
                    // Busts Cloudinary's CDN cache, so a deleted cover cannot
                    // keep serving from the edge after the row is gone.
                    Invalidate = true,
                    ResourceType = ResourceType.Image,
                    Type = "upload"
                };

                var result = await _cloudinary.DestroyAsync(parameters);

                if (string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase))
                    return true;

                // "not found" is a successful no-op: callers treat an already
                // missing asset as deleted.
                if (string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
                    return false;

                _logger.LogWarning(
                    "Cloudinary delete for public id {PublicId} returned {Result}",
                    publicId,
                    result.Result);

                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Cloudinary delete failed for public id {PublicId}", publicId);
                return false;
            }
        }

        public async Task<CloudinaryAssetInfo?> GetAsync(string publicId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return null;

            try
            {
                var resource = await _cloudinary.GetResourceAsync(publicId, cancellationToken);
                if (resource is null || !string.Equals(resource.PublicId, publicId, StringComparison.Ordinal))
                    return null;

                return new CloudinaryAssetInfo(
                    resource.PublicId,
                    resource.SecureUrl?.ToString(),
                    resource.Bytes,
                    resource.Width,
                    resource.Height);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // "Not found" and a transient API error look the same from
                // here; both read as "nothing stored under this id" and the
                // caller decides what that means.
                _logger.LogDebug(ex, "Cloudinary lookup failed for public id {PublicId}", publicId);
                return null;
            }
        }
    }
}
