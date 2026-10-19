using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface ICredentialService
    {
        Task<IEnumerable<Credential>> GetAllCredentialsAsync();
        Task<Credential?> GetCredentialByIdAsync(int id);
        Task<IEnumerable<Credential>> GetCredentialsByTabAsync(string tab);
        Task<IEnumerable<Credential>> GetActiveCredentialsAsync();
        Task<Credential> CreateCredentialAsync(Credential credential);
        Task<Credential> UpdateCredentialAsync(Credential credential);
        Task DeleteCredentialAsync(int id);
    }
}
