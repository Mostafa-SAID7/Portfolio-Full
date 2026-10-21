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
        /// Upload an optimized WebP cover image (max 10MB). The dashboard converts
        /// and resizes supported image selections to WebP before sending them here.
        /// Returns <c>{ url, fileName, size }</c> where <c>url</c> is the
        /// relative path to store on the entity (e.g. /api/files/project-title-hash.webp).
        /// </summary>
        [HttpPost("image")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile? file, [FromForm] string? projectTitle)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Choose an image file to upload." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _storage.SaveImageAsync(stream, file.FileName, projectTitle);
                var storedSize = await _storage.GetFileSizeAsync(url);
                return Ok(new { url, fileName = Path.GetFileName(url), size = storedSize });
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
