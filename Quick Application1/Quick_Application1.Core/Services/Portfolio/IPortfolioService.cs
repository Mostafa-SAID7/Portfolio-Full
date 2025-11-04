// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

using Quick_Application1.Core.Models.Portfolio;

namespace Quick_Application1.Core.Services.Portfolio
{
    public interface IPortfolioService
    {
        // Portfolio Sections
        Task<IEnumerable<PortfolioSection>> GetAllSectionsAsync();
        Task<PortfolioSection?> GetSectionByNameAsync(string name);
        Task<PortfolioSection?> GetSectionByIdAsync(int id);
        Task<PortfolioSection> CreateSectionAsync(PortfolioSection section);
        Task<PortfolioSection> UpdateSectionAsync(PortfolioSection section);
        Task DeleteSectionAsync(int id);

        // Portfolio Items
        Task<IEnumerable<PortfolioItem>> GetAllItemsAsync();
        Task<IEnumerable<PortfolioItem>> GetItemsByTypeAsync(PortfolioItemType itemType);
        Task<PortfolioItem?> GetItemByIdAsync(int id);
        Task<IEnumerable<PortfolioItem>> GetFeaturedItemsAsync();
        Task<PortfolioItem> CreateItemAsync(PortfolioItem item);
        Task<PortfolioItem> UpdateItemAsync(PortfolioItem item);
        Task DeleteItemAsync(int id);

        // Utility methods
        Task<bool> SectionExistsAsync(string name);
        Task<bool> ItemExistsAsync(int id);
        Task ReorderSectionsAsync(Dictionary<int, int> sectionOrder);
        Task ReorderItemsAsync(Dictionary<int, int> itemOrder);
    }
}