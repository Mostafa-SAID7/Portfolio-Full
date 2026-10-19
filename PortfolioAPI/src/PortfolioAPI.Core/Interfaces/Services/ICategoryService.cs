using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface ICategoryService
    {
        /// <summary>All managed categories, ordered (Order, then name).</summary>
        Task<IEnumerable<Category>> GetAllCategoriesAsync();

        /// <summary>How many projects use each category (for the admin list).</summary>
        Task<Dictionary<string, int>> GetUsageCountsAsync();

        /// <summary>Active categories merged with the ones projects still use.</summary>
        Task<IEnumerable<Category>> GetPublicCategoriesAsync();

        Task<Category> GetCategoryByIdAsync(int id);

        Task<Category> CreateCategoryAsync(Category category);

        Task UpdateCategoryAsync(Category category);

        Task DeleteCategoryAsync(int id);

        /// <summary>
        /// Makes sure a category name exists in the managed list. Projects keep
        /// their category as plain text, so a new one typed in the dashboard is
        /// registered here and becomes manageable (and visible on the site).
        /// </summary>
        Task EnsureCategoryAsync(string? name);
    }
}
