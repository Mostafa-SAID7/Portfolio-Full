using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
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

            var result = await _projectService.UpdateAsync(id, dto);
            return result.Success ? NoContent() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var result = await _projectService.DeleteAsync(id);
            return result.Success ? NoContent() : NotFound(result);
        }

        [HttpPatch("{id}/toggle-featured")]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var result = await _projectService.ToggleFeaturedAsync(id);
            return result.Success ? Ok(new { isFeatured = result.Data }) : NotFound(result);
        }
    }
}
