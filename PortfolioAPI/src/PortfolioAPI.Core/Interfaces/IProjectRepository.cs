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

        /// <summary>The distinct category names used by the projects.</summary>
        Task<List<string>> GetCategoryNamesAsync();

        /// <summary>How many projects use the given category.</summary>
        Task<int> CountByCategoryAsync(string category);

        /// <summary>
        /// Rewrites the category of every project that uses it. Executed as a
        /// single UPDATE in the database, so no entity is loaded and rewritten
        /// (which would write every column back and could clobber a concurrent
        /// edit). Returns the number of affected projects.
        /// </summary>
        Task<int> RenameCategoryAsync(string previousName, string newName);

        /// <summary>
        /// How many projects use each category (one grouped query, so the admin
        /// list can show the usage without an N+1 round trip).
        /// </summary>
        Task<Dictionary<string, int>> GetCategoryUsageAsync();
    }
}
