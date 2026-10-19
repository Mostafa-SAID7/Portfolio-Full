using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// Project-specific query surface. The text/technology searches must run
    /// entirely in SQL (issue #17): <c>Technologies</c> is stored as a
    /// comma-separated string column, and EF Core cannot translate collection
    /// operators (<c>Any</c>/<c>Contains</c>) over a value-converted column —
    /// translating them in the service layer would throw at query time.
    /// </summary>
    public interface IProjectRepository : IRepository<Project>
    {
        /// <summary>
        /// Case-insensitive LIKE search across title, description and the
        /// comma-separated technologies column — executed by the database.
        /// </summary>
        Task<List<Project>> SearchAsync(string searchTerm);

        /// <summary>
        /// Active projects whose comma-separated technologies column contains
        /// the given needle — executed by the database.
        /// </summary>
        Task<List<Project>> GetByTechnologyAsync(string technology);
    }
}
