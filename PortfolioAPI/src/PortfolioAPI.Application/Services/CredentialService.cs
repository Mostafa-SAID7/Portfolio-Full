using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class CredentialService : ICredentialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<Credential> _validator;

        public CredentialService(IUnitOfWork unitOfWork, IValidator<Credential> validator)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<IEnumerable<Credential>> GetAllCredentialsAsync()
        {
            return await _unitOfWork.Credentials.GetAllAsync();
        }

        public async Task<Credential?> GetCredentialByIdAsync(int id)
        {
            var credential = await _unitOfWork.Credentials.GetByIdAsync(id);
            if (credential == null)
            {
                throw new NotFoundException($"Credential with id {id} not found.");
            }
            return credential;
        }

        public async Task<IEnumerable<Credential>> GetCredentialsByTabAsync(string tab)
        {
            return await _unitOfWork.Credentials.FindAsync(c => c.Tab == tab);
        }

        public async Task<IEnumerable<Credential>> GetActiveCredentialsAsync()
        {
            return await _unitOfWork.Credentials.FindAsync(c => c.IsActive);
        }

        public async Task<Credential> CreateCredentialAsync(Credential credential)
        {
            var validationResult = await _validator.ValidateAsync(credential);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var createdCredential = await _unitOfWork.Credentials.AddAsync(credential);
            await _unitOfWork.SaveChangesAsync();
            return createdCredential;
        }

        public async Task<Credential> UpdateCredentialAsync(Credential credential)
        {
            var existingCredential = await _unitOfWork.Credentials.GetByIdAsync(credential.Id);
            if (existingCredential == null)
            {
                throw new NotFoundException($"Credential with id {credential.Id} not found.");
            }

            var validationResult = await _validator.ValidateAsync(credential);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            await _unitOfWork.Credentials.UpdateAsync(credential);
            await _unitOfWork.SaveChangesAsync();
            return credential;
        }

        public async Task DeleteCredentialAsync(int id)
        {
            var credential = await _unitOfWork.Credentials.GetByIdAsync(id);
            if (credential == null)
            {
                throw new NotFoundException($"Credential with id {id} not found.");
            }

            await _unitOfWork.Credentials.DeleteAsync(credential);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
