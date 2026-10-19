using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class SkillService : ISkillService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Skill> _validator;

        public SkillService(IUnitOfWork unitOfWork, IValidator<Skill> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Skill>> GetAllSkillsAsync()
        {
            return await _unitOfWork.Skills.GetAllAsync();
        }

        public async Task<Skill?> GetSkillByIdAsync(int id)
        {
            var skill = await _unitOfWork.Skills.GetByIdAsync(id);
            if (skill == null)
            {
                throw new NotFoundException($"Skill with id {id} not found.");
            }
            return skill;
        }

        public async Task<IEnumerable<Skill>> GetSkillsByCategoryAsync(string category)
        {
            return await _unitOfWork.Skills.FindAsync(s => s.Category == category);
        }

        public async Task<IEnumerable<Skill>> GetVisibleSkillsAsync()
        {
            return await _unitOfWork.Skills.FindAsync(s => s.IsActive);
        }

        public async Task<Skill> CreateSkillAsync(Skill skill)
        {
            var validationResult = await _validator.ValidateAsync(skill);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            skill.CreatedAt = DateTime.UtcNow;
            var createdSkill = await _unitOfWork.Skills.AddAsync(skill);
            await _unitOfWork.SaveChangesAsync();
            return createdSkill;
        }

        public async Task UpdateSkillAsync(Skill skill)
        {
            var existingSkill = await _unitOfWork.Skills.GetByIdAsync(skill.Id);
            if (existingSkill == null)
            {
                throw new NotFoundException($"Skill with id {skill.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(skill);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            skill.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Skills.UpdateAsync(skill);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteSkillAsync(int id)
        {
            var skill = await _unitOfWork.Skills.GetByIdAsync(id);
            if (skill == null)
            {
                throw new NotFoundException($"Skill with id {id} not found.");
            }

            await _unitOfWork.Skills.DeleteAsync(skill);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}