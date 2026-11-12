using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Interfaces
{
    public interface IExperienceService
    {
        Task<IEnumerable<Experience>> GetAllExperiencesAsync();
        /// <summary>Public surface: active, non-deleted entries only (issue #33).</summary>
        Task<IEnumerable<Experience>> GetPublicExperiencesAsync();
        Task<Experience> GetExperienceByIdAsync(int id);
        Task<IEnumerable<Experience>> GetCurrentExperiencesAsync();
        Task<Experience> CreateExperienceAsync(Experience experience);
        Task UpdateExperienceAsync(Experience experience);
        Task DeleteExperienceAsync(int id);
    }
}

