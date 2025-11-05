using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Project> _validator;

        public ProjectService(IUnitOfWork unitOfWork, IValidator<Project> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _unitOfWork.Projects.GetAllAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                throw new NotFoundException($"Project with id {id} not found.");
            }
            return project;
        }

        public async Task<IEnumerable<Project>> GetFeaturedProjectsAsync()
        {
            return await _unitOfWork.Projects.FindAsync(p => p.IsFeatured);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            var validationResult = await _validator.ValidateAsync(project);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            project.CreatedAt = DateTime.UtcNow;
            var createdProject = await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveChangesAsync();
            return createdProject;
        }

        public async Task UpdateProjectAsync(Project project)
        {
            var existingProject = await _unitOfWork.Projects.GetByIdAsync(project.Id);
            if (existingProject == null)
            {
                throw new NotFoundException($"Project with id {project.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(project);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            project.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Projects.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(id);
            if (project == null)
            {
                throw new NotFoundException($"Project with id {id} not found.");
            }

            await _unitOfWork.Projects.DeleteAsync(project);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}