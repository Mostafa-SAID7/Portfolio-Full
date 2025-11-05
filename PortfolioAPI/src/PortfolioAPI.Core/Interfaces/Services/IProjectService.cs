using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.DTOs;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface IProjectService
    {
        Task<Result<ProjectDto>> GetByIdAsync(int id);
        Task<Result<IEnumerable<ProjectDto>>> GetAllAsync();
        Task<Result<IEnumerable<ProjectDto>>> GetFeaturedAsync();
        Task<Result<IEnumerable<ProjectDto>>> GetByTechnologyAsync(string technology);
        Task<Result<IEnumerable<ProjectDto>>> SearchAsync(string searchTerm);
        Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto);
        Task<Result<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto);
        Task<Result<bool>> DeleteAsync(int id);
        Task<Result<bool>> ToggleFeaturedAsync(int id);
    }
}