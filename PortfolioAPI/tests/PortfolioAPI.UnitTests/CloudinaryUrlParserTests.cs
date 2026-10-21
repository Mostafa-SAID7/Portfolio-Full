using FluentAssertions;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// The console hands out ONE CLOUDINARY_URL variable; these tests lock the
    /// strict parsing so a malformed value can never put the API in a
    /// half-configured state.
    /// </summary>
    public class CloudinaryUrlParserTests
    {
        [Fact]
        public void TryParseCloudinaryUrl_ParsesTheDocumentedShape()
        {
            var ok = CloudinaryOptions.TryParseCloudinaryUrl(
                "cloudinary://123456789012345:AbCdEfGhIjKlMnOpQr@my-cloud-name",
                out var options);

            ok.Should().BeTrue();
            options.CloudName.Should().Be("my-cloud-name");
            options.ApiKey.Should().Be("123456789012345");
            options.ApiSecret.Should().Be("AbCdEfGhIjKlMnOpQr");
            options.Folder.Should().Be("portfolio");
        }

        [Fact]
        public void TryParseCloudinaryUrl_RejectsValuesThatAreNotCloudinaryUrls()
        {
            foreach (var url in new[]
            {
                null,
                "",
                "https://res.cloudinary.com/vwwqklc0/image/upload/v1/a.png",
                "cloudinary://key-only@cloud",
                "cloudinary://:secret@cloud",
                "cloudinary://key:@cloud",
                "cloud.://pass"
            })
            {
                var ok = CloudinaryOptions.TryParseCloudinaryUrl(url, out _);

                ok.Should().BeFalse(because: $"'{url ?? "null"}' must be rejected");
            }
        }

        [Fact]
        public void TryParseCloudinaryUrl_KeepsEverythingEmptyOnFailure()
        {
            CloudinaryOptions.TryParseCloudinaryUrl("not-a-cloudinary-url", out var options);

            options.IsConfigured.Should().BeFalse();
            options.CloudName.Should().BeEmpty();
            options.ApiKey.Should().BeEmpty();
            options.ApiSecret.Should().BeEmpty();
        }
    }
}
