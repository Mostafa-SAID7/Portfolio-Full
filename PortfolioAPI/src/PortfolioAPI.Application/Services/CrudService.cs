using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    /// <summary>
    /// Shared CRUD skeleton for the five entity services (Skill, Experience,
    /// Contact, Testimonial, Credential). Each of those services was a
    /// ~90 % byte-for-byte clone of the others; the identical policy now lives
    /// here exactly once:
    ///   - get-by-id throws NotFoundException (single message format),
    ///   - create/update validate via FluentValidation and throw
    ///     ValidationException (mapped to 400 by the exception middleware),
    ///   - persistence happens through one UnitOfWork.SaveChangesAsync.
    /// Derived classes only express what is genuinely distinctive: their
    /// filtered queries and their create/update specifics.
    /// </summary>
    public abstract class CrudService<TEntity> where TEntity : BaseEntity
    {
        protected readonly IUnitOfWork UnitOfWork;
        private readonly IValidator<TEntity> _validator;

        protected CrudService(IUnitOfWork unitOfWork, IValidator<TEntity> validator)
        {
            UnitOfWork = unitOfWork;
            _validator = validator;
        }

        /// <summary>The repository backing this aggregate (resolved lazily so unit-test mocks can be configured after construction).</summary>
        protected abstract IRepository<TEntity> Repository { get; }

        /// <summary>Entity display name used in "not found" messages (e.g. "Skill").</summary>
        protected virtual string EntityName => typeof(TEntity).Name;

        /// <summary>Fetch or throw the shared NotFoundException.</summary>
        protected async Task<TEntity> GetRequiredAsync(int id)
        {
            var entity = await Repository.GetByIdAsync(id);
            return entity ?? throw new NotFoundException($"{EntityName} with id {id} not found.");
        }

        /// <summary>Run FluentValidation; throw the shared ValidationException on failure.</summary>
        protected async Task ValidateOrThrowAsync(TEntity entity)
        {
            var validationResult = await _validator.ValidateAsync(entity);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        /// <summary>Validate → stage → commit → return the created entity.</summary>
        protected async Task<TEntity> CreateValidatedAsync(TEntity entity)
        {
            await ValidateOrThrowAsync(entity);
            var created = await Repository.AddAsync(entity);
            await UnitOfWork.SaveChangesAsync();
            return created;
        }

        /// <summary>
        /// The one update strategy: fetch the tracked instance, apply the
        /// incoming values onto it (never attach a second instance with the
        /// same key), validate, stamp UpdatedAt, commit.
        /// </summary>
        protected async Task<TEntity> UpdateTrackedAsync(int id, Action<TEntity> apply)
        {
            var existing = await GetRequiredAsync(id);
            apply(existing);
            await ValidateOrThrowAsync(existing);
            existing.UpdatedAt = DateTime.UtcNow;
            await Repository.UpdateAsync(existing);
            await UnitOfWork.SaveChangesAsync();
            return existing;
        }

        /// <summary>Fetch (throwing when missing) → stage delete → commit.</summary>
        protected async Task DeleteOrThrowAsync(int id)
        {
            var entity = await GetRequiredAsync(id);
            await Repository.DeleteAsync(entity);
            await UnitOfWork.SaveChangesAsync();
        }
    }
}
