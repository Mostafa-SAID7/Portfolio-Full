using FluentValidation;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    public class ExperienceService : CrudService<Experience>, IExperienceService
    {
        public ExperienceService(IUnitOfWork unitOfWork, IValidator<Experience> validator)
            : base(unitOfWork, validator)
        {
        }

        protected override IRepository<Experience> Repository => UnitOfWork.Experiences;

        public async Task<IEnumerable<Experience>> GetAllExperiencesAsync()
        {
            return await UnitOfWork.Experiences.GetAllAsync();
        }

        /// <summary>
        /// Public surface: active, non-deleted entries only. The filter runs in
        /// SQL (issue #33); the ordering stays in memory because the public
        /// list is small and the order is a display concern.
        /// </summary>
        public async Task<IEnumerable<Experience>> GetPublicExperiencesAsync()
        {
            var experiences = await UnitOfWork.Experiences.FindAsync(e => e.IsActive && !e.IsDeleted);
            return experiences
                .OrderBy(e => e.Order)
                .ThenByDescending(e => e.StartDate)
                .ToList();
        }

        public async Task<Experience> GetExperienceByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<IEnumerable<Experience>> GetCurrentExperiencesAsync()
        {
            return await UnitOfWork.Experiences.FindAsync(e => e.Current);
        }

        public async Task<Experience> CreateExperienceAsync(Experience experience)
        {
            return await CreateValidatedAsync(experience);
        }

        public async Task UpdateExperienceAsync(Experience experience)
        {
            await UpdateTrackedAsync(experience.Id, existing => existing.Apply(experience));
        }

        public async Task DeleteExperienceAsync(int id)
        {
            await DeleteOrThrowAsync(id);
        }
    }
}
