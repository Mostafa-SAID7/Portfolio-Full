using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// The migration is the operational entry point for moving covers to
    /// Cloudinary; these tests lock down its idempotency and failure
    /// behaviour so it can be safely re-run against production data.
    /// </summary>
    public class MediaMigrationServiceTests : IDisposable
    {
        private const string CloudinaryUrl =
            "https://res.cloudinary.com/demo/image/upload/v1737500000/portfolio/projects/haxel-footwear/cover.png";

        private readonly string _tempUploadRoot;
        private readonly string _webpFile;
        private readonly List<Project> _projects = new();

        public MediaMigrationServiceTests()
        {
            _tempUploadRoot = Path.Combine(Path.GetTempPath(), "migration-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempUploadRoot);
            _webpFile = Path.Combine(_tempUploadRoot, "haxel-footwear-1280.webp");

            using var image = new Image<Rgba32>(4, 4);
            using (var buffer = File.Create(_webpFile))
            {
                image.Save(buffer, new WebpEncoder());
            }
        }

        public void Dispose()
        {
            try { Directory.Delete(Path.GetDirectoryName(_tempUploadRoot)!, recursive: true); }
            catch (IOException) { /* best effort cleanup */ }
        }

        private PortfolioDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase($"migration-{Guid.NewGuid():N}")
                .Options;

            var context = new PortfolioDbContext(options);
            context.Projects.AddRange(_projects);
            context.SaveChanges();
            _projects.Clear();
            return context;
        }

        private Mock<IFileStorageService> CloudinaryStorageMock()
        {
            var storage = new Mock<IFileStorageService>();
            storage.SetupGet(s => s.Provider).Returns(MediaProvider.Cloudinary);
            return storage;
        }

        private IConfiguration Configuration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileStorage:Path"] = _tempUploadRoot,
                    ["Cloudinary:CloudName"] = "demo"
                })
                .Build();

        private MediaMigrationService CreateService(PortfolioDbContext db, Mock<IFileStorageService> storage) =>
            new(
                db,
                storage.Object,
                new ImageValidator(Configuration()),
                Configuration(),
                NullLogger<MediaMigrationService>.Instance);

        [Fact]
        public async Task RunAsync_WithLocalActiveProvider_IsANoOpWithASingleFailureLine()
        {
            var storage = CloudinaryStorageMock();
            storage.SetupGet(s => s.Provider).Returns(MediaProvider.Local);
            _projects.Add(new Project { Id = 1, Slug = "haxel", ImageUrl = "/api/files/haxel-footwear-1280.webp" });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.Failed.Should().Be(1);
            report.Failures.Single().Should().Contain("Set Storage:Provider=cloudinary");
        }

        [Fact]
        public async Task RunAsync_SkipsRowsAlreadyOnCloudinary_Idempotent()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project
            {
                Id = 1,
                Slug = "haxel",
                ImageUrl = CloudinaryUrl,
                StorageProvider = MediaProvider.Cloudinary,
                StoragePublicId = "portfolio/projects/haxel/cover"
            });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.AlreadyOnProvider.Should().Be(1);
            report.Migrated.Should().Be(0);
            storage.Verify(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_MigratesALocalCover_UploadingAndUpdatingTheRow()
        {
            var storage = CloudinaryStorageMock();
            var uploaded = new StoredImage(
                MediaProvider.Cloudinary,
                "portfolio/projects/haxel-footwear/cover",
                CloudinaryUrl,
                CloudinaryUrl,
                CloudinaryUrl,
                "cover.png",
                2048,
                4,
                4);
            storage
                .Setup(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploaded);

            _projects.Add(new Project { Id = 1, Slug = "haxel-footwear", ImageUrl = "/api/files/haxel-footwear-1280.webp" });
            using var db = CreateDbContext();
            var project = await db.Projects.SingleAsync();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.Migrated.Should().Be(1);
            report.Failed.Should().Be(0);

            var reloaded = await db.Projects.AsNoTracking().SingleAsync();
            reloaded.ImageUrl.Should().Be(CloudinaryUrl);
            reloaded.StorageProvider.Should().Be(MediaProvider.Cloudinary);
            reloaded.StoragePublicId.Should().Be("portfolio/projects/haxel-footwear/cover");

            // The local file is the rollback source — migration never deletes it.
            File.Exists(_webpFile).Should().BeTrue();
        }

        [Fact]
        public async Task RunAsync_InDryRun_ModeCountsButChangesNothing()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project { Id = 1, Slug = "haxel-footwear", ImageUrl = "/api/files/haxel-footwear-1280.webp" });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: true);

            report.DryRun.Should().BeTrue();
            report.Migrated.Should().Be(1);
            storage.Verify(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            var untouched = await db.Projects.AsNoTracking().SingleAsync();
            untouched.ImageUrl.Should().Be("/api/files/haxel-footwear-1280.webp");
            untouched.StorageProvider.Should().BeNull();
        }

        [Fact]
        public async Task RunAsync_BackfillsThePublicId_ForALegacyCloudinaryUrl()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project { Id = 1, Slug = "haxel", ImageUrl = CloudinaryUrl });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            // No re-upload: the asset already lives on the CDN.
            report.AlreadyOnProvider.Should().Be(1);
            report.Migrated.Should().Be(0);
            storage.Verify(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            var backfilled = await db.Projects.AsNoTracking().SingleAsync();
            backfilled.StorageProvider.Should().Be(MediaProvider.Cloudinary);
            backfilled.StoragePublicId.Should().Be("portfolio/projects/haxel-footwear/cover");
        }

        [Fact]
        public async Task RunAsync_CountsExternalUrlsWithoutTouchingThem()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project
            {
                Id = 1,
                Slug = "haxel",
                ImageUrl = "https://avatars.githubusercontent.com/u/1?v=4"
            });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.External.Should().Be(1);
            storage.Verify(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_TreatsAForeignCloudinaryUrlAsExternalMedia_OursIsNotTouched()
        {
            // A different account under the same marker: the row predates the
            // storage columns, but back-filling it with the recovered public id
            // would make a later delete aim at one of OUR assets.
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project
            {
                Id = 1,
                Slug = "haxel",
                ImageUrl = "https://res.cloudinary.com/other/image/upload/v1/portfolio/projects/haxel/cover.png"
            });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.External.Should().Be(1);
            report.AlreadyOnProvider.Should().Be(0);
            storage.Verify(s => s.UploadImageAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_ReportsAMissingLocalFileInsteadOfFailingTheRun()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project { Id = 1, Slug = "haxel-footwear", ImageUrl = "/api/files/missing-1280.webp" });
            using var db = CreateDbContext();

            var report = await CreateService(db, storage).RunAsync(dryRun: false);

            report.Failed.Should().Be(1);
            report.Failures.Single().Should().Contain("is missing");
            report.Failed.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RunAsync_PropagatesCancellation()
        {
            var storage = CloudinaryStorageMock();
            _projects.Add(new Project { Id = 1, Slug = "haxel-footwear", ImageUrl = "/api/files/haxel-footwear-1280.webp" });
            using var db = CreateDbContext();
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            var act = () => CreateService(db, storage).RunAsync(dryRun: false, cancelled.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
