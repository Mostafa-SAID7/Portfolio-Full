using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Services;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// Uploads are stored in the DATABASE (shared by every API instance), not
    /// in one web server's folder — the dev API and the production API share
    /// the rows but not the disks, which is why a cover uploaded through one
    /// deployment used to 404 on the other.
    /// </summary>
    public class FileStorageServiceTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly string _uploadRoot;
        private readonly PortfolioDbContext _context;
        private readonly FileStorageService _service;

        public FileStorageServiceTests()
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            _context = new PortfolioDbContext(options);

            // A throw-away folder stands in for App_Data/uploads.
            _uploadRoot = Path.Combine(Path.GetTempPath(), "portfolio-upload-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_uploadRoot);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileStorage:Path"] = _uploadRoot,
                    ["FileStorage:BaseUrl"] = "/api/files",
                    ["FileStorage:MaxFileSize"] = "10485760"
                })
                .Build();

            _service = new FileStorageService(_context, configuration);
        }

        public void Dispose()
        {
            _context.Dispose();
            try { Directory.Delete(_uploadRoot, recursive: true); } catch { /* best effort */ }
        }

        /// <summary>A minimal valid PNG (8-byte signature + payload).</summary>
        private static byte[] PngBytes() =>
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        [Fact]
        public async Task SaveImageAsync_StoresBytesInTheDatabase_AndReturnsRelativeUrl()
        {
            using var stream = new MemoryStream(PngBytes());

            var url = await _service.SaveImageAsync(stream, "cover.png");

            url.Should().StartWith("/api/files/");
            url.Should().EndWith(".png");

            var stored = await _context.StoredFiles.SingleAsync();
            stored.FileName.Should().Be(Path.GetFileName(url));
            stored.ContentType.Should().Be("image/png");
            stored.Size.Should().Be(PngBytes().Length);
            stored.Content.Should().Equal(PngBytes());
        }

        [Fact]
        public async Task GetStoredFileAsync_ReturnsTheStoredFile()
        {
            using var stream = new MemoryStream(PngBytes());
            var url = await _service.SaveImageAsync(stream, "cover.png");

            var stored = await _service.GetStoredFileAsync(url);

            stored.Should().NotBeNull();
            stored!.ContentType.Should().Be("image/png");
            stored.Content.Should().Equal(PngBytes());
        }

        [Fact]
        public async Task GetStoredFileAsync_ReturnsNull_ForAnUnknownFile()
        {
            var stored = await _service.GetStoredFileAsync("/api/files/does-not-exist.png");

            stored.Should().BeNull();
        }

        [Fact]
        public async Task GetFileAsync_ReturnsTheBytes()
        {
            using var stream = new MemoryStream(PngBytes());
            var url = await _service.SaveImageAsync(stream, "cover.png");

            using var file = await _service.GetFileAsync(url);
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);

            buffer.ToArray().Should().Equal(PngBytes());
        }

        [Fact]
        public async Task GetFileAsync_Throws_ForAnUnknownFile()
        {
            Func<Task> act = async () => await _service.GetFileAsync("/api/files/missing.png");

            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [Fact]
        public async Task DeleteFileAsync_RemovesTheRow()
        {
            using var stream = new MemoryStream(PngBytes());
            var url = await _service.SaveImageAsync(stream, "cover.png");

            await _service.DeleteFileAsync(url);

            (await _context.StoredFiles.AnyAsync()).Should().BeFalse();
            (await _service.FileExistsAsync(url)).Should().BeFalse();
        }

        [Fact]
        public async Task FileExistsAsync_And_GetFileSizeAsync_ReflectTheStoredFile()
        {
            using var stream = new MemoryStream(PngBytes());
            var url = await _service.SaveImageAsync(stream, "cover.png");

            (await _service.FileExistsAsync(url)).Should().BeTrue();
            (await _service.GetFileSizeAsync(url)).Should().Be(PngBytes().Length);
        }

        [Fact]
        public async Task GetStoredFileAsync_ImportsALegacyFileFromDisk()
        {
            // A file that only exists on disk (uploaded before the
            // database-backed store) must be imported on first access so the
            // other API instance can serve it too.
            var legacyName = "legacy-cover.png";
            await File.WriteAllBytesAsync(Path.Combine(_uploadRoot, legacyName), PngBytes());

            var stored = await _service.GetStoredFileAsync($"/api/files/{legacyName}");

            stored.Should().NotBeNull();
            stored!.FileName.Should().Be(legacyName);
            (await _context.StoredFiles.AnyAsync(f => f.FileName == legacyName)).Should().BeTrue();
        }

        [Fact]
        public async Task SaveImageAsync_RejectsANonImagePayload()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));

            Func<Task> act = async () => await _service.SaveImageAsync(stream, "evil.png");

            await act.Should().ThrowAsync<InvalidOperationException>();
            (await _context.StoredFiles.AnyAsync()).Should().BeFalse();
        }
    }
}
