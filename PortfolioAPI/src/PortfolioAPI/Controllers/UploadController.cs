using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.Interfaces;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Backend-owned image uploads (project covers) — admin only
    /// (issue #7: unauthenticated uploads allowed disk/content abuse).
    ///
    /// The response carries BOTH the delivery URLs the frontends consume and
    /// the storage metadata (provider + public id) that must be persisted with
    /// the entity, so a later replace/delete can name the exact asset. No
    /// provider detail leaks here: the controller only talks to
    /// <see cref="IFileStorageService"/>.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IFileStorageService _storage;

        public UploadController(IFileStorageService storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Upload a cover image (max 10MB). The dashboard converts and resizes
        /// supported selections to WebP before sending them here.
        ///
        /// Returns <c>{ url, url640, url1280, fileName, size, provider, publicId, width, height }</c>.
        /// <c>url</c> is what goes on the entity; for Cloudinary it is an
        /// absolute HTTPS CDN URL, for local storage a relative /api/files path.
        /// <paramref name="oldImageUrl"/> is accepted for backward compatibility
        /// only — replacing an asset is owned by the project update, which
        /// removes the previous cover only after the new record is committed.
        /// </summary>
        [HttpPost("image")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            IFormFile? file,
            [FromForm] string? projectTitle,
            [FromForm] string? slug,
            [FromForm] string? oldImageUrl,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Choose an image file to upload." });
            }

            try
            {
                await using var stream = file.OpenReadStream();

                var stored = await _storage.UploadImageAsync(
                    new ImageUploadRequest
                    {
                        Content = stream,
                        FileName = file.FileName,
                        // The project slug scopes the stored asset id; the title
                        // is the fallback when the dashboard has no slug yet.
                        Slug = slug ?? projectTitle,
                        AssetType = "cover"
                    },
                    cancellationToken);

                return Ok(new
                {
                    url = stored.Url,
                    url640 = stored.Url640,
                    url1280 = stored.Url1280,
                    fileName = stored.FileName,
                    size = stored.Bytes,
                    provider = stored.Provider,
                    publicId = stored.PublicId,
                    width = stored.Width,
                    height = stored.Height
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
