using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class ExperienceService : IExperienceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Experience> _validator;

        public ExperienceService(IUnitOfWork unitOfWork, IValidator<Experience> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Experience>> GetAllExperiencesAsync()
        {
            return await _unitOfWork.Experiences.GetAllAsync();
        }

        public async Task<Experience?> GetExperienceByIdAsync(int id)
        {
            var experience = await _unitOfWork.Experiences.GetByIdAsync(id);
            if (experience == null)
            {
                throw new NotFoundException($"Experience with id {id} not found.");
            }
            return experience;
        }

        public async Task<IEnumerable<Experience>> GetCurrentExperiencesAsync()
        {
            return await _unitOfWork.Experiences.FindAsync(e => e.Current);
        }

        public async Task<Experience> CreateExperienceAsync(Experience experience)
        {
            var validationResult = await _validator.ValidateAsync(experience);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            experience.CreatedAt = DateTime.UtcNow;
            var createdExperience = await _unitOfWork.Experiences.AddAsync(experience);
            await _unitOfWork.SaveChangesAsync();
            return createdExperience;
        }

        public async Task UpdateExperienceAsync(Experience experience)
        {
            var existingExperience = await _unitOfWork.Experiences.GetByIdAsync(experience.Id);
            if (existingExperience == null)
            {
                throw new NotFoundException($"Experience with id {experience.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(experience);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            experience.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Experiences.UpdateAsync(experience);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteExperienceAsync(int id)
        {
            var experience = await _unitOfWork.Experiences.GetByIdAsync(id);
            if (experience == null)
            {
                throw new NotFoundException($"Experience with id {id} not found.");
            }

            await _unitOfWork.Experiences.DeleteAsync(experience);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}