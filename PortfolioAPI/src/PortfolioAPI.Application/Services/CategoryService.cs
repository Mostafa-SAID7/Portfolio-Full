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
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
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
                managed.Select(c => c.Name),
                StringComparer.OrdinalIgnoreCase);

            var extra = usage.Keys
                .Where(name => !known.Contains(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new Category { Name = name, IsActive = true, Order = int.MaxValue });

            // Only categories that are actually in use become filter chips -
            // an empty chip would filter the list down to nothing.
            return managed
                .Concat(extra)
                .Where(c => usage.ContainsKey(c.Name))
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            var name = category.Name.Trim();
            if (await NameExistsAsync(name, excludeId: null))
            {
                throw new ValidationException($"A category named '{name}' already exists.");
            }

            category.Name = name;
            return await CreateValidatedAsync(category);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            var existing = await GetRequiredAsync(category.Id);
            var name = category.Name.Trim();

            if (await NameExistsAsync(name, excludeId: existing.Id))
            {
                throw new ValidationException($"A category named '{name}' already exists.");
            }

            var previousName = existing.Name;

            // Hiding a category that projects still use would silently drop
            // those projects out of the public filter chips - refuse it.
            if (existing.IsActive && !category.IsActive)
            {
                var used = await CountProjectsUsingAsync(existing.Name);
                if (used > 0)
                {
                    throw new ValidationException(
                        $"'{existing.Name}' is still used by {used} project{(used == 1 ? "" : "s")} - " +
                        "reassign them before hiding it.");
                }
            }

            await UpdateTrackedAsync(existing.Id, current =>
            {
                current.Apply(category.Name, category.Order, category.IsActive);
            });

            // Keep the projects in sync: a renamed category must not leave
            // projects pointing at a name that no longer exists.
            if (!string.Equals(previousName, name, StringComparison.OrdinalIgnoreCase))
            {
                await RenameProjectsAsync(previousName, name);
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await GetRequiredAsync(id);

            var used = await CountProjectsUsingAsync(category.Name);
            if (used > 0)
            {
                throw new ValidationException(
                    $"'{category.Name}' is still used by {used} project{(used == 1 ? "" : "s")} - " +
                    "rename or reassign them before deleting it.");
            }

            await DeleteOrThrowAsync(id);
        }

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
                Name = trimmed,
                IsActive = true,
                Order = await NextOrderAsync()
            };

            await UnitOfWork.Categories.AddAsync(category);
            await UnitOfWork.SaveChangesAsync();
        }

        private async Task<bool> NameExistsAsync(string name, int? excludeId)
        {
            var matches = await UnitOfWork.Categories.FindAsync(
                c => c.Name.ToLower() == name.ToLower());

            return matches.Any(c => excludeId == null || c.Id != excludeId);
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
