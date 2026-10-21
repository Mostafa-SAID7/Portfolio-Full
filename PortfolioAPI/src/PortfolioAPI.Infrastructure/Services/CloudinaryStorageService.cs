using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortfolioAPI.Core.Interfaces;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// Cloudinary-backed implementation of <see cref="IFileStorageService"/>.
    ///
    /// The bytes are uploaded ONCE, unresized: Cloudinary generates every
    /// derivative at delivery time from the stored original, so there is no
    /// need to keep several physical copies and no need to re-encode on upload.
    ///
    /// The asset identity is the Cloudinary <b>public id</b> (persisted in the
    /// database), never the delivery URL. The URL contains a version segment
    /// that changes on every replace, so it is a delivery detail, not an
    /// identity.
    /// </summary>
    public sealed class CloudinaryStorageService : IFileStorageService
    {
        /// <summary>Delivery transformations, applied by inserting them into the upload URL.</summary>
        internal const string UploadMarker = "/image/upload/";
        internal const string BaseTransformation = "f_auto,q_auto";
        internal const string Width640Transformation = "f_auto,q_auto,c_limit,w_640";
        internal const string Width1280Transformation = "f_auto,q_auto,c_limit,w_1280";

        private const string AssetUnavailable = "The image could not be stored in the media store.";
        private const string DefaultFolder = "portfolio";

        private readonly ICloudinaryGateway _gateway;
        private readonly IImageValidator _validator;
        private readonly CloudinaryOptions _options;
        private readonly string _localUploadPath;

        public CloudinaryStorageService(
            ICloudinaryGateway gateway,
            IImageValidator validator,
            IConfiguration configuration,
            CloudinaryOptions options)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _localUploadPath = UploadStorage.ResolveRoot(configuration);
        }

        public string Provider => MediaProvider.Cloudinary;

        public async Task<StoredImage> UploadImageAsync(ImageUploadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Content);

            // Same validator the local provider runs: extension, content type,
            // magic bytes, size, pixel dimensions and a full decode.
            var validated = await _validator.ValidateAsync(request.Content, request.FileName, null, cancellationToken);

            // Reserve an id this upload can own. The base is deterministic
            // (project slug + asset name), and a counter is appended only when
            // something else already owns it - so a second project can never
            // silently overwrite the first one's cover. A transient lookup
            // failure aborts here (the raw exception becomes the generic
            // message) instead of silently misjudging the id.
            string publicId;
            try
            {
                publicId = await ReservePublicIdAsync(BuildPublicId(request), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
            {
                throw new InvalidOperationException(AssetUnavailable, ex);
            }

            var outcome = await _gateway.UploadAsync(new MemoryStream(validated.Content), publicId, overwrite: false, cancellationToken);
            if (!outcome.Success || string.IsNullOrWhiteSpace(outcome.SecureUrl))
                throw new InvalidOperationException(outcome.Error ?? AssetUnavailable);

            // Cloudinary may have assigned its own id; store what it actually used.
            var storedPublicId = string.IsNullOrWhiteSpace(outcome.PublicId) ? publicId : outcome.PublicId;

            return new StoredImage(
                MediaProvider.Cloudinary,
                storedPublicId,
                outcome.SecureUrl!,
                WithTransformation(outcome.SecureUrl, Width640Transformation)!,
                WithTransformation(outcome.SecureUrl, Width1280Transformation)!,
                Path.GetFileName(storedPublicId),
                outcome.Bytes > 0 ? outcome.Bytes : validated.Content.LongLength,
                outcome.Width > 0 ? outcome.Width : validated.Width,
                outcome.Height > 0 ? outcome.Height : validated.Height);
        }

        public async Task<StoredImage> ReplaceImageAsync(
            ImageUploadRequest request,
            MediaReference previous,
            CancellationToken cancellationToken = default)
        {
            // The new asset goes up first and is fully verified before anything
            // is removed. If the upload fails the previous asset is still
            // intact, so the project never shows a broken image.
            var stored = await UploadImageAsync(request, cancellationToken);

            if (previous.IsEmpty || !previous.IsCloudinary)
            {
                // Nothing Cloudinary-side to clean up (a legacy local cover is
                // removed by the project update, which owns the DB commit).
                return stored;
            }

            // Replacing in place: the id did not change, so Cloudinary already
            // swapped the bytes under the same public id - nothing to delete.
            if (string.Equals(previous.PublicId, stored.PublicId, StringComparison.Ordinal))
                return stored;

            await DeleteImageAsync(previous, cancellationToken);
            return stored;
        }

        public async Task<bool> DeleteImageAsync(MediaReference reference, CancellationToken cancellationToken = default)
        {
            if (reference.IsEmpty)
                return false;

            // A reference says who OWNS the asset, and that is not always the
            // active provider: rows written before Cloudinary was enabled still
            // point at local files.
            if (!reference.IsCloudinary)
            {
                if (string.IsNullOrWhiteSpace(reference.Url))
                    return false;

                LocalUploadFiles.TryDeleteStoredImage(reference.Url, _localUploadPath);
                return true;
            }

            var publicId = !string.IsNullOrWhiteSpace(reference.PublicId)
                ? reference.PublicId!
                : // URL recovery only for URLs that belong to THIS cloud:
                  // the id a foreign domain yields must never reach a delete.
                  LooksLikeCloudinaryUrl(reference.Url, _options.CloudName)
                    ? TryGetPublicId(reference.Url)
                    : null;

            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            // A delete request can only ever target THIS folder. Public ids
            // are minted by BuildPublicId under it, and a stale/foreign value
            // that travelled through a client payload must not name an
            // arbitrary asset in the account.
            var managedPrefix = SanitizeSegment(_options.Folder, DefaultFolder) + "/";
            if (!publicId.StartsWith(managedPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            // (publicId resolved and scoped: issue the delete)
            return await _gateway.DeleteAsync(publicId, cancellationToken);
        }

        public string GetImageUrl(MediaReference reference) => reference.Url ?? string.Empty;

        /// <summary>
        /// Structural check for a Cloudinary delivery URL belonging to THIS
        /// cloud (<c>res.cloudinary.com/{cloudName}/image/upload/...</c>).
        /// Used by the media migration to recognise rows that already point at
        /// our CDN. Fails closed: without a configured cloud name nothing is
        /// classified as ours, so a foreign domain that merely contains the
        /// marker can never inherit a public id that a later delete could aim
        /// at one of our assets.
        /// </summary>
        public static bool LooksLikeCloudinaryUrl(string? url, string? cloudName)
        {
            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            return Regex.IsMatch(
                url,
                $@"^https://res\.cloudinary\.com/{Regex.Escape(cloudName)}/image/upload/",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Recovers the public id from a URL this API produced. Last resort for
        /// rows that predate the public id column; a missing/foreign URL yields
        /// null rather than a guess.
        /// </summary>
        public static string? TryGetPublicId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var index = url.IndexOf(UploadMarker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return null;

            var path = url[(index + UploadMarker.Length)..];
            var suffixIndex = path.IndexOfAny(new[] { '?', '#' });
            if (suffixIndex >= 0)
                path = path[..suffixIndex];

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return null;

            // Drop a version segment and any transformation segments.
            var publicIdSegments = segments
                .SkipWhile(s => Regex.IsMatch(s, "^v[0-9]+$") || s.Contains('=') || s.Contains(','))
                .ToArray();

            if (publicIdSegments.Length == 0)
                return null;

            var publicId = string.Join('/', publicIdSegments);
            var lastSlash = publicId.LastIndexOf('/');
            var dot = publicId.LastIndexOf('.');
            return dot > lastSlash ? publicId[..dot] : publicId;
        }

        /// <summary>
        /// Deterministic, sanitised public id:
        /// <c>{folder}/projects/{slug}/{assetType}/{fileName}</c>.
        ///
        /// Every segment is reduced to <c>a-z0-9-</c>, so a file name or title
        /// can never inject a path separator, a traversal or an arbitrary
        /// Cloudinary location. The id carries no extension: the format is a
        /// delivery decision (f_auto picks it per browser).
        /// </summary>
        private string BuildPublicId(ImageUploadRequest request)
        {
            var folder = SanitizeSegment(_options.Folder, DefaultFolder);
            var scope = SanitizeSegment(request.Slug, "project");
            var assetType = SanitizeSegment(request.AssetType, "cover");
            var name = SanitizeSegment(Path.GetFileNameWithoutExtension(request.FileName), "cover");

            return $"{folder}/projects/{scope}/{assetType}/{name}";
        }

        /// <summary>
        /// Picks an unused public id, appending -2, -3, ... while something
        /// already owns the base. One lookup per upload; the local provider
        /// solves the same problem with a file-exists check.
        /// </summary>
        private async Task<string> ReservePublicIdAsync(string basePublicId, CancellationToken cancellationToken)
        {
            var candidate = basePublicId;

            for (var suffix = 2; suffix <= 50; suffix++)
            {
                if (await _gateway.GetAsync(candidate, cancellationToken) is null)
                    return candidate;

                candidate = $"{basePublicId}-{suffix}";
            }

            return $"{basePublicId}-{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// Turns a delivery URL into a transformed one by inserting the
        /// transformation right after <c>/image/upload/</c> and dropping the
        /// format extension Cloudinary appended - <c>f_auto</c> negotiates the
        /// format itself, so the URL must not pin one.
        /// </summary>
        internal static string? WithTransformation(string? secureUrl, string transformation)
        {
            if (string.IsNullOrWhiteSpace(secureUrl))
                return null;

            var index = secureUrl.IndexOf(UploadMarker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return secureUrl; // unexpected shape: hand it back untouched

            var head = secureUrl[..(index + UploadMarker.Length)];
            var tail = secureUrl[(index + UploadMarker.Length)..];

            var suffix = string.Empty;
            var suffixIndex = tail.IndexOfAny(new[] { '?', '#' });
            if (suffixIndex >= 0)
            {
                suffix = tail[suffixIndex..];
                tail = tail[..suffixIndex];
            }

            return $"{head}{transformation}/{StripFormatExtension(tail)}{suffix}";
        }

        /// <summary>
        /// Removes <c>.png</c>/<c>.jpg</c>/... from the last path segment, which
        /// is the format Cloudinary appended to the public id.
        /// </summary>
        private static string StripFormatExtension(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
                return path;

            var dot = path.LastIndexOf('.');
            return dot > lastSlash ? path[..dot] : path;
        }

        private static string SanitizeSegment(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var normalized = value.Normalize(System.Text.NormalizationForm.FormD).ToLowerInvariant();
            var sanitized = Regex.Replace(normalized, @"[^a-z0-9]+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(sanitized))
                return fallback;

            return sanitized.Length > 100 ? sanitized[..100].TrimEnd('-') : sanitized;
        }

        // ------------------------------------------------------------------
        // Legacy file-oriented surface.
        //
        // These predate the provider-agnostic media operations. Cloudinary is
        // neither a document store nor a file server, so they keep their
        // signatures but say so plainly instead of pretending to work.
        // ------------------------------------------------------------------

        public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType) =>
            throw new NotSupportedException("Use UploadImageAsync: Cloudinary stores images, not arbitrary files.");

        public Task<string> SaveImageAsync(Stream imageStream, string fileName, string? projectTitle = null) =>
            throw new NotSupportedException("Use UploadImageAsync.");

        public Task<string> SaveDocumentAsync(Stream documentStream, string fileName) =>
            throw new NotSupportedException("Use UploadImageAsync: Cloudinary stores images, not documents.");

        public Task<(string Url640, string Url1280)> SaveOptimizedResponsiveImageAsync(
            Stream imageStream,
            string? projectSlug = null,
            string? oldImageUrl = null) =>
            throw new NotSupportedException("Use UploadImageAsync: Cloudinary generates responsive variants at delivery time.");

        public Task<Stream> GetFileAsync(string fileUrl) =>
            throw new NotSupportedException("Stored images are delivered by the Cloudinary CDN and never streamed back through this API.");

        public Task DeleteFileAsync(string fileUrl) =>
            DeleteImageAsync(new MediaReference(fileUrl, TryGetPublicId(fileUrl), MediaProvider.Cloudinary));

        public Task<bool> FileExistsAsync(string fileUrl) =>
            FileExistsCoreAsync(fileUrl);

        public async Task<long> GetFileSizeAsync(string fileUrl)
        {
            var publicId = TryGetPublicId(fileUrl);
            var asset = string.IsNullOrWhiteSpace(publicId) ? null : await _gateway.GetAsync(publicId);

            if (asset is null)
                throw new FileNotFoundException("File not found", fileUrl);

            return asset.Bytes;
        }

        private async Task<bool> FileExistsCoreAsync(string fileUrl)
        {
            var publicId = TryGetPublicId(fileUrl);
            return !string.IsNullOrWhiteSpace(publicId) && await _gateway.GetAsync(publicId) is not null;
        }
    }
}
