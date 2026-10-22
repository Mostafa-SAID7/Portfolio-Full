using FluentValidation;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    /// <summary>
    /// Manages the project categories shown in the dashboard and used as filter
    /// chips on the public portfolio.
    ///
    /// A project keeps its category as plain text (no breaking change for the
    /// portfolio or the dashboard), so this service keeps the managed list and
    /// the project data in sync: renaming a category rewrites the projects
    /// that use it, and deleting one is refused while projects still use it.
    /// </summary>
    public class CategoryService : CrudService<Category>, ICategoryService
    {
        public CategoryService(IUnitOfWork unitOfWork, IValidator<Category> validator)
            : base(unitOfWork, validator)
        {
        }

        protected override IRepository<Category> Repository => UnitOfWork.Categories;

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = await UnitOfWork.Categories.GetAllAsync();
            return categories
                .OrderBy(c => c.Order)
                .ThenBy(c => c.NameEn, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<Dictionary<string, int>> GetUsageCountsAsync()
        {
            return await UnitOfWork.Projects.GetCategoryUsageAsync();
        }

        public async Task<IEnumerable<Category>> GetPublicCategoriesAsync()
        {
            var managed = (await UnitOfWork.Categories.FindAsync(c => c.IsActive)).ToList();
            var usage = await UnitOfWork.Projects.GetCategoryUsageAsync();

            // Categories that are still used by a project but not managed (yet)
            // must stay visible - dropping them would silently remove a filter
            // chip and hide projects from the public site.
            var known = new HashSet<string>(
                managed.Select(c => c.NameEn),
                StringComparer.OrdinalIgnoreCase);

            var extra = usage.Keys
                .Where(name => !known.Contains(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new Category { NameEn = name, IsActive = true, Order = int.MaxValue });

            // Only categories that are actually in use become filter chips -
            // an empty chip would filter the list down to nothing.
            return managed
                .Concat(extra)
                .Where(c => usage.ContainsKey(c.NameEn))
                .OrderBy(c => c.Order)
                .ThenBy(c => c.NameEn, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            var nameEn = category.NameEn.Trim();
            var nameAr = category.NameAr.Trim();
            if (await NameExistsAsync(nameEn, excludeId: null))
            {
                throw new ValidationException($"A category named '{nameEn}' already exists.");
            }

            category.NameEn = nameEn;
            category.NameAr = nameAr;
            return await CreateValidatedAsync(category);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            var existing = await GetRequiredAsync(category.Id);
            var nameEn = category.NameEn.Trim();
            var nameAr = category.NameAr.Trim();

            if (await NameExistsAsync(nameEn, excludeId: existing.Id))
            {
                throw new ValidationException($"A category named '{nameEn}' already exists.");
            }

            var previousName = existing.NameEn;

            // Hiding a category that projects still use would silently drop
            // those projects out of the public filter chips - refuse it.
            if (existing.IsActive && !category.IsActive)
            {
                var used = await CountProjectsUsingAsync(existing.NameEn);
                if (used > 0)
                {
                    throw new ValidationException(
                        $"'{existing.NameEn}' is still used by {used} project{(used == 1 ? "" : "s")} - " +
                        "reassign them before hiding it.");
                }
            }

            await UpdateTrackedAsync(existing.Id, current =>
            {
                current.Apply(nameEn, nameAr, category.Order, category.IsActive);
            });

            // Keep the projects in sync: a renamed category must not leave
            // projects pointing at a name that no longer exists.
            if (!string.Equals(previousName, nameEn, StringComparison.OrdinalIgnoreCase))
            {
                await RenameProjectsAsync(previousName, nameEn);
            }
        }

        public async Task PatchCategoryAsync(int id, PortfolioAPI.Core.DTOs.UpdateCategoryPatchDto patch)
        {
            var existing = await GetRequiredAsync(id);
            var nameEn = patch.NameEn?.Trim() ?? existing.NameEn;
            var nameAr = patch.NameAr?.Trim() ?? existing.NameAr;
            var previousName = existing.NameEn;

            if (patch.NameEn is not null && await NameExistsAsync(nameEn, excludeId: id))
            {
                throw new ValidationException($"A category named '{nameEn}' already exists.");
            }

            if (patch.IsActive == false && existing.IsActive)
            {
                var used = await CountProjectsUsingAsync(existing.NameEn);
                if (used > 0)
                {
                    throw new ValidationException(
                        $"'{existing.NameEn}' is still used by {used} project{(used == 1 ? "" : "s")} - " +
                        "reassign them before hiding it.");
                }
            }

            await UpdateTrackedAsync(id, current =>
            {
                if (patch.NameEn is not null) current.NameEn = nameEn;
                if (patch.NameAr is not null) current.NameAr = nameAr;
                if (patch.Order.HasValue) current.Order = patch.Order.Value;
                if (patch.IsActive.HasValue) current.IsActive = patch.IsActive.Value;
            });

            if (patch.NameEn is not null && !string.Equals(previousName, nameEn, StringComparison.OrdinalIgnoreCase))
            {
                await RenameProjectsAsync(previousName, nameEn);
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await GetRequiredAsync(id);

            var used = await CountProjectsUsingAsync(category.NameEn);
            if (used > 0)
            {
                throw new ValidationException(
                    $"'{category.NameEn}' is still used by {used} project{(used == 1 ? "" : "s")} - " +
                    "rename or reassign them before deleting it.");
            }

            await DeleteOrThrowAsync(id);
        }

        /// <summary>
        /// Stages a new managed category if the name is free. Deliberately does
        /// NOT save: callers that write other aggregates in the same operation
        /// (Project create/update) commit everything in ONE SaveChanges, which
        /// keeps "project + its category" atomic (the previous behavior saved
        /// here, splitting the unit of work in half).
        /// </summary>
        public async Task EnsureCategoryAsync(string? name)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return;
            }

            if (await NameExistsAsync(trimmed, excludeId: null))
            {
                return;
            }

            var category = new Category
            {
                NameEn = trimmed,
                NameAr = string.Empty,
                IsActive = true,
                Order = await NextOrderAsync()
            };

            await UnitOfWork.Categories.AddAsync(category);
        }

        private async Task<bool> NameExistsAsync(string name, int? excludeId)
        {
            // SQL EXISTS — the old FindAsync(...).Any() loaded every matching
            // category row into memory just to answer "does it exist?".
            return await UnitOfWork.Categories.AnyAsync(
                c => c.NameEn.ToLower() == name.ToLower() &&
                     (excludeId == null || c.Id != excludeId));
        }

        private async Task<int> NextOrderAsync()
        {
            var categories = await UnitOfWork.Categories.GetAllAsync();
            return categories.Any() ? categories.Max(c => c.Order) + 1 : 0;
        }

        private async Task<int> CountProjectsUsingAsync(string name)
        {
            return await UnitOfWork.Projects.CountByCategoryAsync(name);
        }

        private async Task RenameProjectsAsync(string previousName, string newName)
        {
            // One UPDATE in the database: no entity is loaded, so no other
            // column of the project is rewritten.
            await UnitOfWork.Projects.RenameCategoryAsync(previousName, newName);
        }
    }
}
