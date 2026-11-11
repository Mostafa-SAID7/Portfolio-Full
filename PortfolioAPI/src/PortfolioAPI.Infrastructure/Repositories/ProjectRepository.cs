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

        public new async Task<Project?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.ProjectWebsites)
                    .ThenInclude(pw => pw.Website)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project?> GetByIdWithWebsitesAsync(int id)
        {
            return await _dbSet
                .Include(p => p.ProjectWebsites)
                    .ThenInclude(pw => pw.Website)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public new async Task<IEnumerable<Project>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.ProjectWebsites)
                    .ThenInclude(pw => pw.Website)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Order)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Project>> GetProjectsByWebsiteAsync(string websiteSlug, string? category = null, string? search = null, bool? featured = null)
        {
            var normalizedSlug = websiteSlug.Trim().ToLowerInvariant();

            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.ProjectWebsites)
                    .ThenInclude(pw => pw.Website)
                .Where(p => p.IsActive && !p.IsDeleted &&
                            p.ProjectWebsites.Any(pw => pw.Website.Slug.ToLower() == normalizedSlug &&
                                                        pw.Website.IsActive &&
                                                        pw.IsPublished));

            if (!string.IsNullOrWhiteSpace(category))
            {
                var normCat = category.Trim().ToLower();
                query = query.Where(p => (p.CategoryEn != null && p.CategoryEn.ToLower() == normCat) ||
                                         (p.CategoryAr != null && p.CategoryAr.ToLower() == normCat));
            }

            if (featured.HasValue)
            {
                query = query.Where(p => p.Featured == featured.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.TitleEn.ToLower().Contains(term) ||
                                         p.TitleAr.ToLower().Contains(term) ||
                                         p.DescriptionEn.ToLower().Contains(term) ||
                                         p.DescriptionAr.ToLower().Contains(term) ||
                                         (p.ClientEn != null && p.ClientEn.ToLower().Contains(term)));
            }

            return await query
                .OrderBy(p => p.Order)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByWebsiteAndSlugAsync(string websiteSlug, string projectSlug)
        {
            var normWeb = websiteSlug.Trim().ToLowerInvariant();
            var normProj = projectSlug.Trim().ToLowerInvariant();

            return await _dbSet
                .AsNoTracking()
                .Include(p => p.ProjectWebsites)
                    .ThenInclude(pw => pw.Website)
                .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted &&
                                          p.Slug.ToLower() == normProj &&
                                          p.ProjectWebsites.Any(pw => pw.Website.Slug.ToLower() == normWeb &&
                                                                      pw.Website.IsActive &&
                                                                      pw.IsPublished));
        }

        public async Task<List<string>> GetCategoriesByWebsiteAsync(string websiteSlug)
        {
            var normWeb = websiteSlug.Trim().ToLowerInvariant();

            return await _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted &&
                            p.CategoryEn != null && p.CategoryEn != string.Empty &&
                            p.ProjectWebsites.Any(pw => pw.Website.Slug.ToLower() == normWeb &&
                                                        pw.Website.IsActive &&
                                                        pw.IsPublished))
                .Select(p => p.CategoryEn!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Project>> SearchAsync(string searchTerm)
        {
            // Match the needle as a substring of title, description or the raw
            // comma-separated technologies column (same semantics as the old
            // in-expression filter: substring anywhere, including mid-item).
            // Column names must match the bilingual schema (TitleEn/Ar, ... -
            // the pre-bilingual [Title]/[Description] columns no longer exist).
            // LIKE wildcards in user input are escaped.
            var like = $"%{EscapeLike(searchTerm)}%";

            return await _dbSet
                .FromSqlInterpolated($"""
                    SELECT * FROM [Projects]
                    WHERE [TitleEn] LIKE {like}
                       OR [DescriptionEn] LIKE {like}
                       OR [Technologies] LIKE {like}
                       OR [TitleAr] LIKE {like}
                       OR [DescriptionAr] LIKE {like}
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

        /// <summary>
        /// SQL EXISTS check — never loads project rows into memory (fix for C1).
        /// Uses case-insensitive comparison to match the unique index behaviour.
        /// </summary>
        public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        {
            var normalized = slug.Trim().ToLowerInvariant();
            return await _dbSet.AnyAsync(p =>
                p.Slug.ToLower() == normalized &&
                (excludeId == null || p.Id != excludeId));
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
