using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IFileStorageService _fileStorage;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectService projectService,
            IFileStorageService fileStorage,
            IConfiguration configuration,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _fileStorage = fileStorage;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
        {
            var result = await _projectService.GetAllAsync();
            return Ok(result.Data);
        }

        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetFeaturedProjects()
        {
            var result = await _projectService.GetFeaturedAsync();
            return Ok(result.Data);
        }

        [HttpGet("technology/{technology}")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjectsByTechnology(string technology)
        {
            var result = await _projectService.GetByTechnologyAsync(technology);
            return Ok(result.Data);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> SearchProjects([FromQuery] string term)
        {
            var result = await _projectService.SearchAsync(term);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var result = await _projectService.GetByIdAsync(id);
            return result.Success && result.Data != null ? Ok(result.Data) : NotFound(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _projectService.CreateAsync(dto);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(
                nameof(GetProject),
                new { id = result.Data.Id },
                result.Data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _projectService.GetByIdAsync(id);
            var result = await _projectService.UpdateAsync(id, dto);

            // Cover replaced: drop the old backend-owned file (external URLs
            // and portfolio-bundled paths are never touched).
            if (result.Success && existing.Data?.ImageUrl is { } previous && previous != dto.ImageUrl)
            {
                await TryDeleteStoredCoverAsync(previous);
            }

            return result.Success ? NoContent() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var existing = await _projectService.GetByIdAsync(id);
            var result = await _projectService.DeleteAsync(id);

            if (result.Success && existing.Data?.ImageUrl is { } cover)
            {
                await TryDeleteStoredCoverAsync(cover);
            }

            return result.Success ? NoContent() : NotFound(result);
        }

        [HttpPatch("{id}/toggle-featured")]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var result = await _projectService.ToggleFeaturedAsync(id);
            return result.Success ? Ok(new { isFeatured = result.Data }) : NotFound(result);
        }

        /// <summary>
        /// Best-effort cleanup of a cover this API owns (/api/files/*).
        /// Never fails the CRUD operation on cleanup problems.
        /// </summary>
        private async Task TryDeleteStoredCoverAsync(string imageUrl)
        {
            if (!UploadStorage.IsManagedFile(imageUrl, _configuration))
            {
                return;
            }

            try
            {
                await _fileStorage.DeleteFileAsync(imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete stored cover {ImageUrl}", imageUrl);
            }
        }
    }
}
