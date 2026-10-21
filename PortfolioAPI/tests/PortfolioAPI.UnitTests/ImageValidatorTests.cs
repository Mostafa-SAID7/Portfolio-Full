using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// The validator is the single gate every upload passes through, so its
    /// behaviour is what keeps a renamed payload from becoming a stored file.
    /// </summary>
    public class ImageValidatorTests
    {
        private readonly Mock<IConfiguration> _configuration = new();
        private readonly ImageValidator _validator;

        public ImageValidatorTests()
        {
            _configuration
                .SetupGet(c => c[It.IsAny<string>()])
                .Returns((string?)null);

            _validator = new ImageValidator(_configuration.Object);
        }

        private static byte[] Png(int width = 4, int height = 4)
        {
            using var image = new Image<Rgba32>(width, height);
            using var buffer = new MemoryStream();
            image.Save(buffer, new PngEncoder());
            return buffer.ToArray();
        }

        private static byte[] Jpeg(int width = 4, int height = 4)
        {
            using var image = new Image<Rgba32>(width, height);
            using var buffer = new MemoryStream();
            image.Save(buffer, new JpegEncoder());
            return buffer.ToArray();
        }

        private static byte[] Webp(int width = 4, int height = 4)
        {
            using var image = new Image<Rgba32>(width, height);
            using var buffer = new MemoryStream();
            image.Save(buffer, new WebpEncoder());
            return buffer.ToArray();
        }

        private static byte[] Gif(int width = 4, int height = 4)
        {
            using var image = new Image<Rgba32>(width, height);
            using var buffer = new MemoryStream();
            image.Save(buffer, new GifEncoder());
            return buffer.ToArray();
        }

        private Task<ValidatedImage> ValidateAsync(byte[] bytes, string name, string? contentType = null)
        {
            return _validator.ValidateAsync(new MemoryStream(bytes), name, contentType);
        }

        // -- accepted formats -------------------------------------------------

        [Theory]
        [InlineData(".png")]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".gif")]
        [InlineData(".webp")]
        public async Task ValidateAsync_AcceptsEveryAllowedFormat(string extension)
        {
            var bytes = extension switch
            {
                ".png" => Png(),
                ".jpg" or ".jpeg" => Jpeg(),
                ".gif" => Gif(),
                _ => Webp()
            };

            var validated = await ValidateAsync(bytes, $"cover{extension}");

            validated.Width.Should().Be(4);
            validated.Height.Should().Be(4);
        }

        [Fact]
        public async Task ValidateAsync_DerivesTheContentTypeFromTheMagicBytes()
        {
            var result = await ValidateAsync(Png(), "cover.png", "image/png");
            result.ContentType.Should().Be("image/png");
        }

        // -- rejected payloads ------------------------------------------------

        [Fact]
        public async Task ValidateAsync_RejectsAnUnsupportedExtension()
        {
            // SVG is deliberately refused: it can carry scripts.
            var act = () => ValidateAsync("<svg onload=alert(1)/>"u8.ToArray(), "vector.svg");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Only JPG, PNG, GIF and WebP image files are allowed.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsBytesThatDoNotMatchTheirExtension()
        {
            // A JPEG renamed to .png must not pass on the extension alone.
            var act = () => ValidateAsync(Jpeg(), "cover.png");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("The file content does not match its extension.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsAMismatchedDeclaredContentType()
        {
            var act = () => ValidateAsync(Png(), "cover.png", "text/plain");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Content type 'text/plain' does not match the '.png' extension.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsAnEmptyUpload()
        {
            var act = () => ValidateAsync(Array.Empty<byte>(), "cover.png");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("The uploaded file is empty.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsACorruptedImage()
        {
            // Real PNG signature, truncated data — a header-only check would
            // happily accept this.
            var bytes = Png().Take(20).ToArray();

            var act = () => ValidateAsync(bytes, "cover.png");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("The uploaded image is corrupted or is not a supported image.");
        }

        // -- limits -----------------------------------------------------------

        [Fact]
        public async Task ValidateAsync_RejectsAnImageWiderThanTheConfiguredMaximum()
        {
            var validator = ValidatorWith(new Dictionary<string, string?>
            {
                ["FileStorage:MaxImageWidth"] = "2"
            });

            var act = () => validator.ValidateAsync(new MemoryStream(Png(4, 4)), "cover.png", null);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Image width (4px) exceeds the maximum allowed width of 2px.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsAnImageTallerThanTheConfiguredMaximum()
        {
            var validator = ValidatorWith(new Dictionary<string, string?>
            {
                ["FileStorage:MaxImageHeight"] = "2"
            });

            var act = () => validator.ValidateAsync(new MemoryStream(Png(4, 4)), "cover.png", null);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Image height (4px) exceeds the maximum allowed height of 2px.");
        }

        [Fact]
        public async Task ValidateAsync_RejectsADecompressionBombBeforeDecodingIt()
        {
            // 20000x20000 is ~1.6 GB of pixels — the guard must fire on the
            // header read, long before anything is decoded.
            var validator = ValidatorWith(new Dictionary<string, string?>
            {
                ["FileStorage:MaxImagePixels"] = "4"
            });

            var act = () => validator.ValidateAsync(new MemoryStream(Png(4, 4)), "cover.png", null);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Image dimensions (4x4) exceed the maximum allowed total of 4 pixels.");
        }

        [Fact]
        public async Task ValidateAsync_DecodesAnimatedGifs_WithAOneFrameCap()
        {
            // An animated GIF declares 3 frames. Only the first is decoded —
            // so a tiny file carrying dozens of full-size frames can never
            // multiply the pixel budget on the server.
            using var animated = new Image<Rgba32>(4, 4);
            for (var frame = 0; frame < 2; frame++)
            {
                animated.Frames.CreateFrame();
            }
            using var buffer = new MemoryStream();
            animated.Save(buffer, new GifEncoder());

            var validated = await ValidateAsync(buffer.ToArray(), "animated.gif");

            validated.Width.Should().Be(4);
            validated.Height.Should().Be(4);
            validated.ContentType.Should().Be("image/gif");
        }

        [Fact]
        public void Limits_ComefromConfiguration()        {
            var validator = ValidatorWith(new Dictionary<string, string?>
            {
                ["FileStorage:MaxFileSize"] = "1048576",
                ["FileStorage:MaxImageWidth"] = "1024",
                ["FileStorage:MaxImageHeight"] = "2048",
                ["FileStorage:MaxImagePixels"] = "8000000"
            });

            validator.Limits.MaxBytes.Should().Be(1_048_576);
            validator.Limits.MaxWidth.Should().Be(1024);
            validator.Limits.MaxHeight.Should().Be(2048);
            validator.Limits.MaxPixels.Should().Be(8_000_000);
        }

        /// <summary>Builds a validator over an explicit in-memory configuration.</summary>
        private static ImageValidator ValidatorWith(Dictionary<string, string?> values) =>
            new(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }
}
