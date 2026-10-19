using FluentValidation;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    /// <summary>
    /// Project management. Implements the DTO/Result-based contract; expected
    /// outcomes (not found, validation) are returned as failed results instead
    /// of exceptions so controllers can map them to proper HTTP responses.
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Project> _validator;

        public ProjectService(IUnitOfWork unitOfWork, IValidator<Project> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Result<ProjectDto>> GetByIdAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            return project == null
                ? Result<ProjectDto>.Fail($"Project with id {id} was not found.")
                : Result<ProjectDto>.Ok(Map(project));
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync()
        {
            var projects = await _unitOfWork.Projects.GetAllAsync();
            var ordered = projects.OrderBy(p => p.Order).ThenByDescending(p => p.CreatedAt);
            return Result<IEnumerable<ProjectDto>>.Ok(ordered.Select(Map).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetFeaturedAsync()
        {
            var projects = await _unitOfWork.Projects.FindAsync(p => p.Featured && p.IsActive);
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(Map).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> GetByTechnologyAsync(string technology)
        {
            var needle = technology?.Trim() ?? string.Empty;
            var projects = await _unitOfWork.Projects.FindAsync(p =>
                p.IsActive && p.Technologies.Any(t => t.Contains(needle)));
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(Map).ToList());
        }

        public async Task<Result<IEnumerable<ProjectDto>>> SearchAsync(string searchTerm)
        {
            var needle = searchTerm?.Trim() ?? string.Empty;
            var projects = await _unitOfWork.Projects.FindAsync(p =>
                p.Title.Contains(needle) ||
                p.Description.Contains(needle) ||
                p.Technologies.Any(t => t.Contains(needle)));
            return Result<IEnumerable<ProjectDto>>.Ok(projects.OrderBy(p => p.Order).Select(Map).ToList());
        }

        public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto)
        {
            var project = new Project
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                ImageUrl = dto.ImageUrl,
                Technologies = Sanitize(dto.Technologies),
                GithubUrl = dto.GithubUrl,
                LiveUrl = dto.LiveUrl,
                Featured = dto.Featured,
                Order = dto.Order,
                IsActive = dto.IsActive,
                Category = NullIfBlank(dto.Category),
                Client = NullIfBlank(dto.Client),
                Type = NullIfBlank(dto.Type),
                Database = NullIfBlank(dto.Database),
                Status = NullIfBlank(dto.Status),
                Badges = Sanitize(dto.Badges),
                Metrics = dto.Metrics?.Select(m => new ProjectMetric
                {
                    Label = m.Label?.Trim() ?? string.Empty,
                    Value = m.Value?.Trim() ?? string.Empty
                }).Where(m => m.Label.Length > 0).ToList() ?? new List<ProjectMetric>(),
                Gradient = NullIfBlank(dto.Gradient),
                CreatedAt = DateTime.UtcNow
            };

            var validation = await _validator.ValidateAsync(project);
            if (!validation.IsValid)
            {
                return Result<ProjectDto>.Fail(
                    "The project could not be saved.",
                    validation.Errors.Select(e => e.ErrorMessage));
            }

            var created = await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<ProjectDto>.Ok(Map(created), "Project created.");
        }

        public async Task<Result<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<ProjectDto>.Fail($"Project with id {id} was not found.");
            }

            project.Title = dto.Title.Trim();
            project.Description = dto.Description.Trim();
            project.ImageUrl = dto.ImageUrl;
            project.Technologies = Sanitize(dto.Technologies);
            project.GithubUrl = dto.GithubUrl;
            project.LiveUrl = dto.LiveUrl;
            project.Featured = dto.Featured;
            project.Order = dto.Order;
            project.IsActive = dto.IsActive;
            project.Category = NullIfBlank(dto.Category);
            project.Client = NullIfBlank(dto.Client);
            project.Type = NullIfBlank(dto.Type);
            project.Database = NullIfBlank(dto.Database);
            project.Status = NullIfBlank(dto.Status);
            project.Badges = Sanitize(dto.Badges);
            project.Metrics = dto.Metrics?.Select(m => new ProjectMetric
            {
                Label = m.Label?.Trim() ?? string.Empty,
                Value = m.Value?.Trim() ?? string.Empty
            }).Where(m => m.Label.Length > 0).ToList() ?? new List<ProjectMetric>();
            project.Gradient = NullIfBlank(dto.Gradient);
            project.UpdatedAt = DateTime.UtcNow;

            var validation = await _validator.ValidateAsync(project);
            if (!validation.IsValid)
            {
                return Result<ProjectDto>.Fail(
                    "The project could not be saved.",
                    validation.Errors.Select(e => e.ErrorMessage));
            }

            await _unitOfWork.Projects.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<ProjectDto>.Ok(Map(project), "Project updated.");
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<bool>.Fail($"Project with id {id} was not found.");
            }

            await _unitOfWork.Projects.DeleteAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Ok(true, "Project deleted.");
        }

        public async Task<Result<bool>> ToggleFeaturedAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                return Result<bool>.Fail($"Project with id {id} was not found.");
            }

            project.Featured = !project.Featured;
            project.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Projects.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Ok(project.Featured);
        }

        private static List<string> Sanitize(List<string>? technologies) =>
            (technologies ?? new List<string>())
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => t.Length > 0)
            .ToList();

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static ProjectDto Map(Project p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            Technologies = p.Technologies?.ToList() ?? new List<string>(),
            GithubUrl = p.GithubUrl,
            LiveUrl = p.LiveUrl,
            Featured = p.Featured,
            Order = p.Order,
            IsActive = p.IsActive,
            Category = p.Category,
            Client = p.Client,
            Type = p.Type,
            Database = p.Database,
            Status = p.Status,
            Badges = p.Badges?.ToList() ?? new List<string>(),
            Metrics = p.Metrics?.Select(m => new ProjectMetricDto { Label = m.Label, Value = m.Value }).ToList()
                      ?? new List<ProjectMetricDto>(),
            Gradient = p.Gradient,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
