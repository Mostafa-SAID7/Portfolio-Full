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

        /// <summary>
        /// Public surface: active, non-deleted projects ordered for display.
        /// The filter runs in SQL so inactive rows are never materialized
        /// (issue #33) - the admin list must keep using <see cref="IRepository{T}.GetAllAsync"/>.
        /// </summary>
        Task<List<Project>> GetPublicProjectsAsync();

        /// <summary>
        /// Returns true when the slug is already taken by another project.
        /// The optional <paramref name="excludeId"/> exempts the project being
        /// updated so its own current slug never blocks a re-save.
        /// Runs entirely in SQL — does NOT load all projects (fix for C1).
        /// </summary>
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);

        /// <summary>
        /// Retrieves active, published projects assigned to the given website slug.
        /// Executed in SQL Server with optional category, technology, search and featured filters.
        /// </summary>
        Task<List<Project>> GetProjectsByWebsiteAsync(string websiteSlug, string? category = null, string? search = null, bool? featured = null);

        /// <summary>
        /// Retrieves a single active project by slug if it is published on the given website.
        /// </summary>
        Task<Project?> GetProjectByWebsiteAndSlugAsync(string websiteSlug, string projectSlug);

        /// <summary>
        /// Retrieves distinct categories used by published projects on the given website.
        /// </summary>
        Task<List<string>> GetCategoriesByWebsiteAsync(string websiteSlug);

        /// <summary>
        /// Loads a project by ID including its ProjectWebsites and Website navigation properties.
        /// </summary>
        Task<Project?> GetByIdWithWebsitesAsync(int id);
    }
}
