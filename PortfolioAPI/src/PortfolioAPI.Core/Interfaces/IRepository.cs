using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// Minimal persistence contract actually consumed by the application
    /// services (ISP): fetch, query, and stage a change — persistence is
    /// committed exclusively through <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Server-side ordered pagination (issue #132): SQL COUNT(*) + a
        /// Skip/Take page query with no-tracking — the full table is never
        /// materialized. Pass a null predicate to page over everything.
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync<TKey>(
            Expression<Func<T, bool>>? predicate,
            Expression<Func<T, TKey>> orderBy,
            bool descending,
            int page,
            int pageSize);

        /// <summary>
        /// Server-side EXISTS — answers "does anything match?" without
        /// materializing the matching rows (the FindAsync(...).Any() pattern
        /// the services used to employ, which loaded every matched row).
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
