using Microsoft.Extensions.Configuration;
using PortfolioAPI.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// The single place every uploaded image is checked, no matter which
    /// provider stores it. The rules are layered so the cheap checks run first
    /// and the expensive ones only on payloads that already look legitimate:
    ///
    /// 1. Extension allowlist      – JPG / PNG / GIF / WebP only (no SVG: it can
    ///    carry scripts and would be served from the API origin).
    /// 2. Content type vs extension – when the client declares one.
    /// 3. Magic bytes              – the signature in the actual bytes must match
    ///    the declared extension, so a renamed executable never passes.
    /// 4. Size cap                 – enforced while streaming, so an over-sized
    ///    upload is rejected before it is buffered.
    /// 5. Pixel dimensions         – read from the HEADER only, so a
    ///    decompression bomb is rejected before a single pixel is decoded.
    /// 6. Full decode              – a truncated/corrupt file can carry a valid
    ///    header; decoding proves the image is actually usable.
    /// </summary>
    public sealed class ImageValidator : IImageValidator
    {
        /// <summary>Extension → delivered content type, format name and magic-byte sniffer.</summary>
        private static readonly IReadOnlyDictionary<string, ImageSignature> AllowedImages =
            new Dictionary<string, ImageSignature>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = new("image/jpeg", "jpeg", IsJpeg),
                [".jpeg"] = new("image/jpeg", "jpeg", IsJpeg),
                [".png"] = new("image/png", "png", IsPng),
                [".gif"] = new("image/gif", "gif", IsGif),
                [".webp"] = new("image/webp", "webp", IsWebp),
            };

        private const long DefaultMaxFileSize = 10 * 1024 * 1024;

        private readonly ImageValidationLimits _limits;

        public ImageValidationLimits Limits => _limits;

        public ImageValidator(IConfiguration configuration)
        {
            _limits = new ImageValidationLimits
            {
                MaxBytes = ReadPositive(configuration, "MaxFileSize", DefaultMaxFileSize),
                MaxWidth = (int)Math.Min(int.MaxValue, ReadPositive(configuration, "MaxImageWidth", 8000)),
                MaxHeight = (int)Math.Min(int.MaxValue, ReadPositive(configuration, "MaxImageHeight", 8000)),
                MaxPixels = ReadPositive(configuration, "MaxImagePixels", 40_000_000)
            };
        }

        public async Task<ValidatedImage> ValidateAsync(
            Stream content,
            string? fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            var extension = string.IsNullOrWhiteSpace(fileName)
                ? null
                : Path.GetExtension(fileName).ToLowerInvariant();

            // Step 1 + 2: extension allowlist, and the declared content type
            // when there is one. A missing file name means the caller only has
            // the raw stream (legacy helpers), so the format comes from the
            // magic bytes alone.
            ImageSignature? signature = null;
            if (extension != null)
            {
                if (!AllowedImages.TryGetValue(extension, out signature!))
                    throw new InvalidOperationException("Only JPG, PNG, GIF and WebP image files are allowed.");

                if (!string.IsNullOrWhiteSpace(contentType) &&
                    !string.Equals(signature.ContentType, contentType.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Content type '{contentType}' does not match the '{extension}' extension.");
            }

            // Steps 3 + 4: capped read (size), then magic bytes.
            var bytes = await ReadCappedAsync(content, cancellationToken);
            if (bytes.Length == 0)
                throw new InvalidOperationException("The uploaded file is empty.");

            if (signature is null)
            {
                signature = DetectSignature(bytes)
                    ?? throw new InvalidOperationException("Only JPG, PNG, GIF and WebP image files are allowed.");
            }
            else if (!signature.Sniff(bytes))
            {
                throw new InvalidOperationException("The file content does not match its extension.");
            }

            // Step 5: header-only dimension read (no pixel allocation).
            ImageInfo? info = TryIdentify(bytes);
            if (info is null)
                throw new InvalidOperationException("The uploaded image is corrupted or is not a supported image.");

            if (info.Width > _limits.MaxWidth)
                throw new InvalidOperationException(
                    $"Image width ({info.Width}px) exceeds the maximum allowed width of {_limits.MaxWidth}px.");

            if (info.Height > _limits.MaxHeight)
                throw new InvalidOperationException(
                    $"Image height ({info.Height}px) exceeds the maximum allowed height of {_limits.MaxHeight}px.");

            if ((long)info.Width * info.Height > _limits.MaxPixels)
                throw new InvalidOperationException(
                    $"Image dimensions ({info.Width}x{info.Height}) exceed the maximum allowed total of {_limits.MaxPixels} pixels.");

            // Step 6: full decode. The header can be perfectly valid while the
            // pixel data is truncated or corrupt. MaxFrames bounds animated
            // formats: a tiny animated GIF can declare hundreds of full-size
            // frames, and covers need none of them.
            try
            {
                using var image = Image.Load(DecodeOptions, bytes);
                return new ValidatedImage(bytes, signature.ContentType, signature.Format, image.Width, image.Height);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                throw new InvalidOperationException("The uploaded image is corrupted or is not a supported image.");
            }
        }

        /// <summary>
        /// Decode options for the corruption probe: one frame is enough to
        /// prove the image decodes, and a frame cap is the thing that keeps an
        /// animated upload from multiplying the pixel budget.
        /// </summary>
        private static readonly DecoderOptions DecodeOptions = new()
        {
            MaxFrames = 1,
            SkipMetadata = true
        };

        private static ImageInfo? TryIdentify(byte[] bytes)
        {
            try
            {
                return Image.Identify(bytes);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Buffers with a hard cap so an over-sized upload is abandoned while it
        /// is being read instead of after it has been fully buffered.
        /// </summary>
        private async Task<byte[]> ReadCappedAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            await using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            long total = 0;
            int read;

            while ((read = await fileStream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken)) > 0)
            {
                total += read;
                if (total > _limits.MaxBytes)
                    throw new InvalidOperationException(
                        $"File size exceeds the maximum allowed size of {_limits.MaxBytes / 1024 / 1024}MB.");

                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }

            return buffer.ToArray();
        }

        private static ImageSignature? DetectSignature(byte[] bytes)
        {
            if (IsJpeg(bytes)) return new("image/jpeg", "jpeg", IsJpeg);
            if (IsPng(bytes)) return new("image/png", "png", IsPng);
            if (IsGif(bytes)) return new("image/gif", "gif", IsGif);
            if (IsWebp(bytes)) return new("image/webp", "webp", IsWebp);
            return null;
        }

        private static long ReadPositive(IConfiguration configuration, string key, long fallback)
        {
            var raw = configuration[$"FileStorage:{key}"];
            return long.TryParse(raw, out var value) && value > 0 ? value : fallback;
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

        private sealed record ImageSignature(string ContentType, string Format, Func<byte[], bool> Sniff);
    }
}
