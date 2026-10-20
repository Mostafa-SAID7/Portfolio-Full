using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Repositories
{
    /// <summary>
    /// Project repository. The two text-search queries are written as raw SQL
    /// (parameterized LIKE) because <c>Technologies</c> is a comma-separated
    /// string column — EF Core cannot translate <c>Technologies.Any(...)</c>
    /// over the converted column (it failed with HTTP 500 before; issue #17).
    /// Filtering now happens entirely in SQL Server.
    /// </summary>
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(PortfolioDbContext context) : base(context)
        {
        }

        public async Task<List<Project>> SearchAsync(string searchTerm)
        {
            // Match the needle as a substring of title, description or the raw
            // comma-separated technologies column (same semantics as the old
            // in-expression filter: substring anywhere, including mid-item).
            // LIKE wildcards in user input are escaped.
            var like = $"%{EscapeLike(searchTerm)}%";

            return await _dbSet
                .FromSqlInterpolated($"""
                    SELECT * FROM [Projects]
                    WHERE [Title] LIKE {like}
                       OR [Description] LIKE {like}
                       OR [Technologies] LIKE {like}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Project>> GetByTechnologyAsync(string technology)
        {
            var like = $"%{EscapeLike(technology)}%";

            return await _dbSet
                .FromSqlInterpolated($"""
                    SELECT * FROM [Projects]
                    WHERE [IsActive] = 1
                      AND [Technologies] LIKE {like}
                    """)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>The distinct category names used by the projects (SQL DISTINCT).</summary>
        public async Task<List<string>> GetCategoryNamesAsync()
        {
            return await _dbSet
                .Where(p => p.CategoryEn != null && p.CategoryEn != string.Empty)
                .Select(p => p.CategoryEn!)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>How many projects use the given category.</summary>
        public async Task<int> CountByCategoryAsync(string category)
        {
            var normalized = category.Trim();
            return await _dbSet.CountAsync(p =>
                p.CategoryEn != null && p.CategoryEn.ToLower() == normalized.ToLower());
        }

        /// <summary>
        /// Rewrites the category of every project that uses it with a single
        /// UPDATE statement - no entity is loaded, so no other column is
        /// written back and a concurrent edit cannot be clobbered.
        /// </summary>
        public async Task<int> RenameCategoryAsync(string previousName, string newName)
        {
            var previous = previousName.Trim();
            var next = newName.Trim();

            return await _dbSet
                .Where(p => p.CategoryEn != null && p.CategoryEn.ToLower() == previous.ToLower())
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.CategoryEn, next));
        }

        /// <summary>How many projects use each category (one grouped query).</summary>
        public async Task<Dictionary<string, int>> GetCategoryUsageAsync()
        {
            var rows = await _dbSet
                .Where(p => p.CategoryEn != null && p.CategoryEn != string.Empty)
                .GroupBy(p => p.CategoryEn!)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            // Case-insensitive: the admin compares the names as typed.
            return rows.ToDictionary(r => r.Category, r => r.Count, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Public surface: active, non-deleted projects ordered for display.
        /// Filtering and ordering happen in SQL (issue #33).
        /// </summary>
        public async Task<List<Project>> GetPublicProjectsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Order)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>T-SQL LIKE escaping: [, % and _ are wildcard characters.</summary>
        private static string EscapeLike(string? value)        {
            var needle = (value ?? string.Empty).Trim();
            return needle
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
        }
    }
}
