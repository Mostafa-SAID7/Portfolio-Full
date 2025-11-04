// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

using Microsoft.EntityFrameworkCore;
using Quick_Application1.Core.Infrastructure;
using Quick_Application1.Core.Models.Portfolio;

namespace Quick_Application1.Core.Services.Portfolio
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;

        public PortfolioService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Portfolio Sections

        public async Task<IEnumerable<PortfolioSection>> GetAllSectionsAsync()
        {
            return await _context.PortfolioSections
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
        }

        public async Task<PortfolioSection?> GetSectionByNameAsync(string name)
        {
            return await _context.PortfolioSections
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower() && s.IsActive);
        }

        public async Task<PortfolioSection?> GetSectionByIdAsync(int id)
        {
            return await _context.PortfolioSections
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        }

        public async Task<PortfolioSection> CreateSectionAsync(PortfolioSection section)
        {
            section.CreatedDate = DateTime.UtcNow;
            section.UpdatedDate = DateTime.UtcNow;

            _context.PortfolioSections.Add(section);
            await _context.SaveChangesAsync();

            return section;
        }

        public async Task<PortfolioSection> UpdateSectionAsync(PortfolioSection section)
        {
            section.UpdatedDate = DateTime.UtcNow;

            _context.PortfolioSections.Update(section);
            await _context.SaveChangesAsync();

            return section;
        }

        public async Task DeleteSectionAsync(int id)
        {
            var section = await _context.PortfolioSections.FindAsync(id);
            if (section != null)
            {
                _context.PortfolioSections.Remove(section);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Portfolio Items

        public async Task<IEnumerable<PortfolioItem>> GetAllItemsAsync()
        {
            return await _context.PortfolioItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<PortfolioItem>> GetItemsByTypeAsync(PortfolioItemType itemType)
        {
            return await _context.PortfolioItems
                .Where(i => i.ItemType == itemType && i.IsActive)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();
        }

        public async Task<PortfolioItem?> GetItemByIdAsync(int id)
        {
            return await _context.PortfolioItems
                .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);
        }

        public async Task<IEnumerable<PortfolioItem>> GetFeaturedItemsAsync()
        {
            return await _context.PortfolioItems
                .Where(i => i.IsFeatured && i.IsActive)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();
        }

        public async Task<PortfolioItem> CreateItemAsync(PortfolioItem item)
        {
            item.CreatedDate = DateTime.UtcNow;
            item.UpdatedDate = DateTime.UtcNow;

            _context.PortfolioItems.Add(item);
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task<PortfolioItem> UpdateItemAsync(PortfolioItem item)
        {
            item.UpdatedDate = DateTime.UtcNow;

            _context.PortfolioItems.Update(item);
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task DeleteItemAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);
            if (item != null)
            {
                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Utility Methods

        public async Task<bool> SectionExistsAsync(string name)
        {
            return await _context.PortfolioSections
                .AnyAsync(s => s.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ItemExistsAsync(int id)
        {
            return await _context.PortfolioItems
                .AnyAsync(i => i.Id == id);
        }

        public async Task ReorderSectionsAsync(Dictionary<int, int> sectionOrder)
        {
            foreach (var (sectionId, order) in sectionOrder)
            {
                var section = await _context.PortfolioSections.FindAsync(sectionId);
                if (section != null)
                {
                    section.DisplayOrder = order;
                    section.UpdatedDate = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task ReorderItemsAsync(Dictionary<int, int> itemOrder)
        {
            foreach (var (itemId, order) in itemOrder)
            {
                var item = await _context.PortfolioItems.FindAsync(itemId);
                if (item != null)
                {
                    item.DisplayOrder = order;
                    item.UpdatedDate = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}