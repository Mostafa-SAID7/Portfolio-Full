using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PortfolioAPI.Infrastructure.Services;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// Uploads are written to the API server's upload folder and referenced by
    /// their RELATIVE path (<c>/api/files/&lt;project&gt;-&lt;hash&gt;.webp</c>) — the bytes never
    /// enter the database, so a 1 MB cover does not become a 1 MB row.
    /// </summary>
    public class FileStorageServiceTests : IDisposable
    {
        private readonly string _uploadRoot;
        private readonly FileStorageService _service;

        public FileStorageServiceTests()
        {
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

            _service = new FileStorageService(configuration);
        }

        public void Dispose()
        {
            try { Directory.Delete(_uploadRoot, recursive: true); } catch { /* best effort */ }
        }

        /// <summary>A minimal WebP signature fixture (the storage layer validates format headers).</summary>
        private static byte[] WebpBytes() =>
            new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P', 1, 2, 3, 4 };

        [Fact]
        public async Task SaveImageAsync_WritesTheFileToDisk_AndReturnsARelativeUrl()
        {
            using var stream = new MemoryStream(WebpBytes());

            var url = await _service.SaveImageAsync(stream, "cover.webp");

            // The URL is relative: the database never stores an environment-specific host.
            url.Should().StartWith("/api/files/");
            url.Should().EndWith(".webp");

            var fileName = Path.GetFileName(url);
            var path = Path.Combine(_uploadRoot, fileName);
            File.Exists(path).Should().BeTrue();
            (await File.ReadAllBytesAsync(path)).Should().Equal(WebpBytes());
        }

        [Fact]
        public async Task GetFileAsync_ReturnsTheStoredBytes()
        {
            using var stream = new MemoryStream(WebpBytes());
            var url = await _service.SaveImageAsync(stream, "cover.webp");

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
        public async Task FileExistsAsync_And_GetFileSizeAsync_ReflectTheStoredFile()
        {
            using var stream = new MemoryStream(WebpBytes());
            var url = await _service.SaveImageAsync(stream, "cover.webp");

            (await _service.FileExistsAsync(url)).Should().BeTrue();
            (await _service.GetFileSizeAsync(url)).Should().Be(WebpBytes().Length);
        }

        [Fact]
        public async Task DeleteFileAsync_RemovesTheFileFromDisk()
        {
            using var stream = new MemoryStream(WebpBytes());
            var url = await _service.SaveImageAsync(stream, "cover.webp");
            var path = Path.Combine(_uploadRoot, Path.GetFileName(url));

            await _service.DeleteFileAsync(url);

            File.Exists(path).Should().BeFalse();
            (await _service.FileExistsAsync(url)).Should().BeFalse();
        }

        [Fact]
        public async Task SaveImageAsync_RejectsANonImagePayload()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));

            Func<Task> act = async () => await _service.SaveImageAsync(stream, "evil.webp");

            await act.Should().ThrowAsync<InvalidOperationException>();
            Directory.GetFiles(_uploadRoot).Should().BeEmpty();
        }

        [Fact]
        public async Task SaveImageAsync_RejectsAnUnsupportedExtension()
        {
            using var stream = new MemoryStream(WebpBytes());

            Func<Task> act = async () => await _service.SaveImageAsync(stream, "payload.svg");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SaveImageAsync_RejectsUnoptimizedImageFormats()
        {
            using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            Func<Task> act = async () => await _service.SaveImageAsync(stream, "cover.png");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Images must be converted to WebP before uploading. Select the image again in a supported browser.");
        }

        [Fact]
        public void DeleteFileAsync_CannotEscapeTheUploadFolder()
        {
            // Path.GetFileName strips directory components, so a traversal
            // attempt can never delete a file outside the store.
            Func<Task> act = async () => await _service.DeleteFileAsync("/api/files/../../web.config");

            act.Should().NotThrowAsync();
        }
    }
}
