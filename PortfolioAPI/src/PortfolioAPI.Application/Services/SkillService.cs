using FluentValidation;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    public class SkillService : CrudService<Skill>, ISkillService
    {
        public SkillService(IUnitOfWork unitOfWork, IValidator<Skill> validator)
            : base(unitOfWork, validator)
        {
        }

        protected override IRepository<Skill> Repository => UnitOfWork.Skills;

        public async Task<IEnumerable<Skill>> GetAllSkillsAsync()
        {
            return await UnitOfWork.Skills.GetAllAsync();
        }

        public async Task<Skill> GetSkillByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<IEnumerable<Skill>> GetSkillsByCategoryAsync(string category)
        {
            return await UnitOfWork.Skills.FindAsync(s => s.CategoryEn == category);
        }

        public async Task<IEnumerable<Skill>> GetVisibleSkillsAsync()
        {
            return await UnitOfWork.Skills.FindAsync(s => s.IsActive);
        }

        public async Task<Skill> CreateSkillAsync(Skill skill)
        {
            return await CreateValidatedAsync(skill);
        }

        public async Task UpdateSkillAsync(Skill skill)
        {
            await UpdateTrackedAsync(skill.Id, existing => existing.Apply(skill));
        }

        public async Task DeleteSkillAsync(int id)
        {
            await DeleteOrThrowAsync(id);
        }
    }
}
