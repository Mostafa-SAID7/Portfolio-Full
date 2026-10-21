using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// The Cloudinary provider is tested through the <see cref="ICloudinaryGateway"/>
    /// seam, so the unit tests exercise the storage logic (id reservation,
    /// replace ordering, delivery URLs, failure handling) without a network.
    /// </summary>
    public class CloudinaryStorageServiceTests
    {
        private const string SecureUrl =
            "https://res.cloudinary.com/demo/image/upload/v1737500000/portfolio/projects/haxel-footwear/cover.png";

        private readonly Mock<ICloudinaryGateway> _gateway = new();
        private readonly CloudinaryOptions _options = new()
        {
            CloudName = "demo",
            ApiKey = "key",
            ApiSecret = "secret",
            Folder = "portfolio"
        };

        private readonly string _tempStoragePath;
        private readonly ImageUploadRequest _request;

        public CloudinaryStorageServiceTests()
        {
            _tempStoragePath = Path.Combine(Path.GetTempPath(), "cloudinary-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempStoragePath);

            _request = new ImageUploadRequest
            {
                Content = new MemoryStream(PngBytes()),
                FileName = "cover.png",
                Slug = "Haxel Footwear",
                AssetType = "cover"
            };
        }

        private CloudinaryStorageService CreateService(bool gatewayHasAsset = false, bool uploadSucceeds = true)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:Path"] = _tempStoragePath })
                .Build();

            _gateway.Setup(g => g.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string publicId, CancellationToken _) =>
                    publicId == "portfolio/projects/haxel-footwear/cover/cover" && gatewayHasAsset
                        ? new CloudinaryAssetInfo(publicId, SecureUrl, 1024, 4, 4)
                        : null);

            _gateway.Setup(g => g.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadSucceeds
                    ? new CloudinaryUploadOutcome(true, "portfolio/projects/haxel-footwear/cover", SecureUrl, "1737500000", "png", 2048, 4, 4, null)
                    : new CloudinaryUploadOutcome(false, null, null, null, null, 0, 0, 0, "The media store rejected the upload (400)."));

            _gateway.Setup(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            return new CloudinaryStorageService(
                _gateway.Object,
                new ImageValidator(configuration),
                configuration,
                _options);
        }

        private static byte[] PngBytes(int width = 4, int height = 4)
        {
            using var image = new Image<Rgba32>(width, height);
            using var buffer = new MemoryStream();
            image.Save(buffer, new PngEncoder());
            return buffer.ToArray();
        }

        [Fact]
        public async Task UploadImageAsync_ReportsTheCloudinaryProvider_AndNeverAHardcodedCloud()
        {
            var service = CreateService();

            var stored = await service.UploadImageAsync(_request);

            stored.Provider.Should().Be(MediaProvider.Cloudinary);
            stored.Url.Should().Be(SecureUrl);
            stored.PublicId.Should().Be("portfolio/projects/haxel-footwear/cover");
            stored.Width.Should().Be(4);
            stored.Height.Should().Be(4);
            stored.Bytes.Should().Be(2048);
        }

        [Fact]
        public async Task UploadImageAsync_BuildsAutoFormatResponsiveUrls_FromTheReturnedUrl()
        {
            var service = CreateService();

            var stored = await service.UploadImageAsync(_request);

            // Transformations are inserted after /image/upload/ and the format
            // extension is stripped, so f_auto can negotiate the best format.
            stored.Url640.Should().Be(
                "https://res.cloudinary.com/demo/image/upload/f_auto,q_auto,c_limit,w_640/v1737500000/portfolio/projects/haxel-footwear/cover");
            stored.Url1280.Should().Be(
                "https://res.cloudinary.com/demo/image/upload/f_auto,q_auto,c_limit,w_1280/v1737500000/portfolio/projects/haxel-footwear/cover");
        }

        [Fact]
        public async Task UploadImageAsync_SanitizesThePublicId_SoUserInputCanNeverEscapeTheFolder()
        {
            var service = CreateService();

            await service.UploadImageAsync(new ImageUploadRequest
            {
                Content = new MemoryStream(PngBytes()),
                FileName = "../../evil name.png",
                Slug = "haxel/../secret",
                AssetType = "cover"
            });

            _gateway.Verify(g => g.UploadAsync(
                It.IsAny<Stream>(),
                "portfolio/projects/haxel-secret/cover/evil-name",
                false,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadImageAsync_ReservesAnUnusedPublicId_WhenTheBaseIsTaken()
        {
            var service = CreateService(gatewayHasAsset: true);

            await service.UploadImageAsync(_request);

            _gateway.Verify(g => g.UploadAsync(
                It.IsAny<Stream>(),
                "portfolio/projects/haxel-footwear/cover/cover-2",
                false,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadImageAsync_NeverOverwrites_OnAFreshUpload()
        {
            var service = CreateService();

            await service.UploadImageAsync(_request);

            _gateway.Verify(g => g.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadImageAsync_ThrowsAFriendlyError_WhenTheGatewayFails()
        {
            var service = CreateService(uploadSucceeds: false);

            var act = () => service.UploadImageAsync(_request);

            // The real Cloudinary reason stays in the server log; the admin
            // gets the short, secret-free message.
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("The media store rejected the upload (400).");
        }

        // -- replacement ------------------------------------------------------

        [Fact]
        public async Task ReplaceImageAsync_UnderTheSamePublicId_DoesNotDeleteTheAsset()
        {
            var service = CreateService();
            var previous = new MediaReference(
                SecureUrl, "portfolio/projects/haxel-footwear/cover", MediaProvider.Cloudinary);

            await service.ReplaceImageAsync(_request, previous);

            _gateway.Verify(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReplaceImageAsync_WithADifferentAsset_DeletesTheOldOneOnlyAfterSuccess()
        {
            var service = CreateService();
            var previous = new MediaReference(
                "https://res.cloudinary.com/demo/image/upload/v1/portfolio/projects/haxel-footwear/old.png",
                "portfolio/projects/haxel-footwear/old",
                MediaProvider.Cloudinary);

            await service.ReplaceImageAsync(_request, previous);

            _gateway.Verify(g => g.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
            _gateway.Verify(g => g.DeleteAsync(
                "portfolio/projects/haxel-footwear/old", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReplaceImageAsync_WhenTheUploadFails_LeavesTheOldAssetIntact()
        {
            var service = CreateService(uploadSucceeds: false);
            var previous = new MediaReference(
                "https://res.cloudinary.com/demo/image/upload/v1/portfolio/projects/haxel-footwear/old.png",
                "portfolio/projects/haxel-footwear/old",
                MediaProvider.Cloudinary);

            var act = () => service.ReplaceImageAsync(_request, previous);

            // The old reference survives a failed upload — the broken-image
            // window the ordering requirement exists to prevent.
            await act.Should().ThrowAsync<InvalidOperationException>();
            _gateway.Verify(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Replace_UNKNOWN_Provider_Reference_DoesNotDeleteAnything()
        {
            // A legacy LOCAL cover is cleaned up by the local provider's rules,
            // never by a Cloudinary delete request.
            await CreateService().ReplaceImageAsync(
                new ImageUploadRequest { Content = new MemoryStream(PngBytes()), FileName = "cover.png", Slug = "haxel" },
                new MediaReference("/api/files/haxel-footwear-1280.webp", null, MediaProvider.Local));

            _gateway.Verify(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // -- deletion ---------------------------------------------------------

        [Fact]
        public async Task DeleteImageAsync_DeletesByPublicId_First()
        {
            var service = CreateService();
            var reference = new MediaReference(SecureUrl, "portfolio/projects/haxel-footwear/cover", MediaProvider.Cloudinary);

            var deleted = await service.DeleteImageAsync(reference);

            deleted.Should().BeTrue();
            _gateway.Verify(g => g.DeleteAsync(
                "portfolio/projects/haxel-footwear/cover", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteImageAsync_WithOnlyALegacyUrl_RecoversThePublicId()
        {
            var service = CreateService();
            var reference = new MediaReference(SecureUrl, null, MediaProvider.Cloudinary);

            var deleted = await service.DeleteImageAsync(reference);

            deleted.Should().BeTrue();
            _gateway.Verify(g => g.DeleteAsync(
                "portfolio/projects/haxel-footwear/cover", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteImageAsync_IgnoresAnUnrecognisedUrl()
        {
            var service = CreateService();
            var reference = new MediaReference("https://example.com/whatever.png", null, MediaProvider.Cloudinary);

            var deleted = await service.DeleteImageAsync(reference);

            deleted.Should().BeFalse();
            _gateway.Verify(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImageAsync_KeepsLocalFilesCoveredByTheLocalProvider()
        {
            // Local rows are cleaned up by the local store; the Cloudinary
            // service must not turn a /api/files URL into a delete call.
            var service = CreateService();
            var reference = new MediaReference("/api/files/haxel-footwear-1280.webp", null, MediaProvider.Local);

            await service.DeleteImageAsync(reference);

            _gateway.Verify(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImageAsync_ReportsFalse_WhenTheAssetWasAlreadyGone()
        {
            var service = CreateService();

            // Configured AFTER CreateService so this setup wins.
            _gateway.Setup(g => g.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var reference = new MediaReference(SecureUrl, "portfolio/projects/haxel-footwear/cover", MediaProvider.Cloudinary);

            var deleted = await service.DeleteImageAsync(reference);

            deleted.Should().BeFalse();
        }

        // -- constructor ------------------------------------------------------

        [Fact]
        public void Constructor_WithIncompleteConfiguration_FailsFastWithoutPrintingValues()
        {
            var act = () => new CloudinaryGateway(
                new CloudinaryOptions { CloudName = "demo", ApiKey = "", ApiSecret = "" },
                NullLogger<CloudinaryGateway>.Instance);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cloudinary configuration is incomplete.");
        }

        // -- helpers ----------------------------------------------------------

        private static IImageValidator NullValidator() => Mock.Of<IImageValidator>();
    }
}
