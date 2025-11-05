using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface IExperienceService
    {
        Task<IEnumerable<Experience>> GetAllExperiencesAsync();
        Task<Experience?> GetExperienceByIdAsync(int id);
        Task<IEnumerable<Experience>> GetCurrentExperiencesAsync();
        Task<Experience> CreateExperienceAsync(Experience experience);
        Task UpdateExperienceAsync(Experience experience);
        Task DeleteExperienceAsync(int id);
    }
}