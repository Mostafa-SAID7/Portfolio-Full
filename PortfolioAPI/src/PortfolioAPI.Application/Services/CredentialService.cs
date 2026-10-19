using FluentValidation;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    public class CredentialService : CrudService<Credential>, ICredentialService
    {
        public CredentialService(IUnitOfWork unitOfWork, IValidator<Credential> validator)
            : base(unitOfWork, validator)
        {
        }

        protected override IRepository<Credential> Repository => UnitOfWork.Credentials;

        public async Task<IEnumerable<Credential>> GetAllCredentialsAsync()
        {
            return await UnitOfWork.Credentials.GetAllAsync();
        }

        public async Task<Credential> GetCredentialByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<IEnumerable<Credential>> GetCredentialsByTabAsync(string tab)
        {
            return await UnitOfWork.Credentials.FindAsync(c => c.Tab == tab);
        }

        public async Task<IEnumerable<Credential>> GetActiveCredentialsAsync()
        {
            return await UnitOfWork.Credentials.FindAsync(c => c.IsActive);
        }

        public async Task<Credential> CreateCredentialAsync(Credential credential)
        {
            return await CreateValidatedAsync(credential);
        }

        public async Task<Credential> UpdateCredentialAsync(Credential credential)
        {
            // Apply onto the tracked instance — attaching the incoming
            // detached entity (the previous behaviour) threw EF's
            // "another instance with the same key ... is already being tracked"
            // and surfaced as an HTTP 500 on every credential update.
            return await UpdateTrackedAsync(credential.Id, existing => existing.Apply(credential));
        }

        public async Task DeleteCredentialAsync(int id)
        {
            await DeleteOrThrowAsync(id);
        }
    }
}
