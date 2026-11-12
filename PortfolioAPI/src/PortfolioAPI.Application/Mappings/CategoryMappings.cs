using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Category entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static CategoryDto ToDto(Category c, int projectCount = 0) => new()
        {
            Id = c.Id,
            NameEn = c.NameEn,
            NameAr = c.NameAr,
            Order = c.Order,
            IsActive = c.IsActive,
            ProjectCount = projectCount
        };

        public static Category ToEntity(CreateCategoryDto dto, int id = 0) => new()
        {
            Id = id,
            NameEn = dto.NameEn?.Trim() ?? string.Empty,
            NameAr = dto.NameAr?.Trim() ?? string.Empty,
            Order = dto.Order,
            IsActive = dto.IsActive
        };
    }
}
