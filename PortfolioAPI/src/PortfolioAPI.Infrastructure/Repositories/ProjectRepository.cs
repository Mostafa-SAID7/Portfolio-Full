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
                .ToListAsync();
        }

        /// <summary>T-SQL LIKE escaping: [, % and _ are wildcard characters.</summary>
        private static string EscapeLike(string? value)
        {
            var needle = (value ?? string.Empty).Trim();
            return needle
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
        }
    }
}
