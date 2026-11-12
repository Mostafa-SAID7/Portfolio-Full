using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Common;

namespace PortfolioAPI.Application.Interfaces
{
    public interface IProjectService
    {
        Task<Result<ProjectDto>> GetByIdAsync(int id);
        Task<Result<IEnumerable<ProjectDto>>> GetAllAsync();
        Task<Result<IEnumerable<ProjectDto>>> GetFeaturedAsync();
        /// <summary>Public surface: active projects only, filtered in SQL (issue #33).</summary>
        Task<Result<IEnumerable<ProjectDto>>> GetPublicProjectsAsync();
        Task<Result<IEnumerable<ProjectDto>>> GetByTechnologyAsync(string technology);
        Task<Result<IEnumerable<ProjectDto>>> SearchAsync(string searchTerm);
        Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto);
        Task<Result<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto);
        Task<Result<bool>> DeleteAsync(int id);
        Task<Result<bool>> ToggleFeaturedAsync(int id);

        Task<Result<IEnumerable<ProjectDto>>> GetProjectsByWebsiteAsync(string websiteSlug, string? category = null, string? search = null, bool? featured = null);
        Task<Result<ProjectDto>> GetProjectByWebsiteAndSlugAsync(string websiteSlug, string projectSlug);
        Task<Result<IEnumerable<string>>> GetCategoriesByWebsiteAsync(string websiteSlug);
        Task<Result<IEnumerable<ProjectWebsiteDto>>> GetWebsiteAssignmentsAsync(int projectId);
        Task<Result<IEnumerable<ProjectWebsiteDto>>> UpdateWebsiteAssignmentsAsync(int projectId, UpdateProjectWebsitesDto dto);
    }
}

