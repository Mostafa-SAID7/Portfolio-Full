using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Backend-owned file uploads (project cover images) — admin only
    /// (issue #7: unauthenticated uploads allowed disk/content abuse).
    ///
    /// Uploaded files are stored under App_Data/uploads and served back at
    /// /api/files/*, and the returned URL is a RELATIVE path — each frontend
    /// resolves it through its own /api proxy, so the database never contains
    /// an environment-specific host and images load over HTTPS everywhere.
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
        /// Upload a cover image (JPG, PNG, GIF, WebP or AVIF; max 10MB).
        /// Returns <c>{ url, fileName, size }</c> where <c>url</c> is the
        /// relative path to store on the entity (e.g. /api/files/&lt;guid&gt;.webp).
        /// </summary>
        [HttpPost("image")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Choose an image file to upload." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _storage.SaveImageAsync(stream, file.FileName);
                return Ok(new { url, fileName = Path.GetFileName(url), size = file.Length });
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
