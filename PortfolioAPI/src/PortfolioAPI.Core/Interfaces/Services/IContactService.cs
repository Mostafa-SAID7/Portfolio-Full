using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface IContactService
    {
        Task<IEnumerable<Contact>> GetAllContactsAsync();
        Task<Contact?> GetContactByIdAsync(int id);
        Task<IEnumerable<Contact>> GetUnreadContactsAsync();
        Task<Contact> CreateContactAsync(Contact contact);
        Task MarkAsReadAsync(int id);
        Task DeleteContactAsync(int id);
    }
}