using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Interfaces
{
    public interface IContactService
    {
        Task<IEnumerable<Contact>> GetAllContactsAsync();
        Task<Contact> GetContactByIdAsync(int id);
        Task<IEnumerable<Contact>> GetUnreadContactsAsync();

        /// <summary>
        /// Server-side ordered + paged inbox (issue #132): newest first, an
        /// optional unread-only filter, SQL Skip/Take — the full table is
        /// never loaded.
        /// </summary>
        Task<PagedResult<Contact>> GetPagedContactsAsync(int page, int pageSize, bool? unread = null);
        Task<Contact> CreateContactAsync(Contact contact);
        Task MarkAsReadAsync(int id);
        Task DeleteContactAsync(int id);
        Task<ContactReply> ReplyToContactAsync(int id, string body);
        Task<IEnumerable<ContactReply>> GetRepliesAsync(int id);
    }
}

