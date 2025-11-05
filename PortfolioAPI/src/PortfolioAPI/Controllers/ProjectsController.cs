using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
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
            var projects = await _projectService.GetAllProjectsAsync();
            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Technologies = p.Technologies,
                ProjectUrl = p.ProjectUrl,
                RepositoryUrl = p.RepositoryUrl,
                ImageUrl = p.ImageUrl,
                IsFeatured = p.IsFeatured,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });
            return Ok(projectDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProjectById(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            var projectDto = new ProjectDto
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Technologies = project.Technologies,
                ProjectUrl = project.ProjectUrl,
                RepositoryUrl = project.RepositoryUrl,
                ImageUrl = project.ImageUrl,
                IsFeatured = project.IsFeatured,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };
            return Ok(projectDto);
        }

        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetFeaturedProjects()
        {
            var projects = await _projectService.GetFeaturedProjectsAsync();
            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Technologies = p.Technologies,
                ProjectUrl = p.ProjectUrl,
                RepositoryUrl = p.RepositoryUrl,
                ImageUrl = p.ImageUrl,
                IsFeatured = p.IsFeatured,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });
            return Ok(projectDtos);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(Project project)
        {
            var createdProject = await _projectService.CreateProjectAsync(project);
            var projectDto = new ProjectDto
            {
                Id = createdProject.Id,
                Title = createdProject.Title,
                Description = createdProject.Description,
                Technologies = createdProject.Technologies,
                ProjectUrl = createdProject.ProjectUrl,
                RepositoryUrl = createdProject.RepositoryUrl,
                ImageUrl = createdProject.ImageUrl,
                IsFeatured = createdProject.IsFeatured,
                StartDate = createdProject.StartDate,
                EndDate = createdProject.EndDate,
                CreatedAt = createdProject.CreatedAt,
                UpdatedAt = createdProject.UpdatedAt
            };
            return CreatedAtAction(nameof(GetProjectById), new { id = projectDto.Id }, projectDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, Project project)
        {
            if (id != project.Id)
            {
                return BadRequest();
            }

            await _projectService.UpdateProjectAsync(project);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            await _projectService.DeleteProjectAsync(id);
            return NoContent();
        }
    }
}