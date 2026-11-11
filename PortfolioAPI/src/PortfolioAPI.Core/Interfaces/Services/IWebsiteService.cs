using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.DTOs;

namespace PortfolioAPI.Core.Interfaces.Services
{
    public interface IWebsiteService
    {
        Task<Result<IEnumerable<WebsiteDto>>> GetAllAsync();
        Task<Result<IEnumerable<WebsiteDto>>> GetActiveWebsitesAsync();
        Task<Result<WebsiteDto>> GetByIdAsync(int id);
        Task<Result<WebsiteDto>> GetBySlugAsync(string slug);
        Task<Result<WebsiteDto>> CreateAsync(CreateWebsiteDto dto);
        Task<Result<WebsiteDto>> UpdateAsync(int id, UpdateWebsiteDto dto);
        Task<Result<bool>> DeleteAsync(int id);
        Task<Result<bool>> ToggleActiveAsync(int id);
    }
}

