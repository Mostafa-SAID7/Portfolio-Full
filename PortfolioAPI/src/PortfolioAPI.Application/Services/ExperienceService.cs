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

            // Copy the incoming values ONTO the instance we just fetched instead
            // of attaching a second entity with the same key (which threw
            // "another instance with the same key ... is already being tracked"
            // and surfaced as a generic HTTP 500). CreatedAt stays server-owned.
            existingExperience.Company = experience.Company;
            existingExperience.Position = experience.Position;
            existingExperience.StartDate = experience.StartDate;
            existingExperience.EndDate = experience.EndDate;
            existingExperience.Description = experience.Description;
            existingExperience.Technologies = experience.Technologies ?? existingExperience.Technologies;
            existingExperience.Location = experience.Location;
            existingExperience.Current = experience.Current;
            existingExperience.Order = experience.Order;
            existingExperience.IsActive = experience.IsActive;

            var validationResult = await _validator.ValidateAsync(existingExperience);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            existingExperience.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Experiences.UpdateAsync(existingExperience);
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