using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository over the scoped DbContext. Implements the six-member
    /// <see cref="IRepository{T}"/> contract (fetch / query / stage change) —
    /// committing belongs to the UnitOfWork. The extra query helpers below are
    /// kept for tests and the specialized <see cref="ProjectRepository"/>.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly PortfolioDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(PortfolioDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            // Read-only list endpoint: no change tracking. EF would otherwise
            // build a snapshot for every row (47 skills + 13 projects per
            // dashboard load) that nobody ever uses.
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.UtcNow;
            var entry = await _dbSet.AddAsync(entity);
            return entry.Entity;
        }

        public Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.UtcNow;

            // Only ATTACH genuinely detached instances. Services hand us the
            // entity they just loaded (already tracked); the old code called
            // _dbSet.Update unconditionally, which marked EVERY column Modified
            // and issued a full-row UPDATE — silently overwriting concurrent
            // edits (lost update) and masking the EF value-comparer warning.
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Update(entity);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        // --- Query helpers (used by unit tests; not on the service contract) ----

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<T>> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
        {
            return await _dbSet
                .Where(predicate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<PagedResult<T>> GetPagedAsync<TKey>(
            Expression<Func<T, bool>>? predicate,
            Expression<Func<T, TKey>> orderBy,
            bool descending,
            int page,
            int pageSize)
        {
            var query = _dbSet.AsNoTracking();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Count the FILTERED set so the page math holds when a predicate is
            // active; without a predicate it is the full unfiltered count.
            var totalCount = await query.CountAsync();

            var ordered = descending
                ? Queryable.OrderByDescending(query, orderBy)
                : Queryable.OrderBy(query, orderBy);

            // page/pageSize are clamped by the controller; Skip/Take alone keep
            // the SQL server-side — no full-table materialization (issue #132).
            var items = await ordered
                .Skip((Math.Max(1, page) - 1) * Math.Max(1, pageSize))
                .Take(Math.Max(1, pageSize))
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, page, pageSize);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}
