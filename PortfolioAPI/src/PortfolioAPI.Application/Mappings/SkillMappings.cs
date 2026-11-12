using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Skill entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static SkillDto ToDto(Skill s) => new()
        {
            Id = s.Id,
            NameEn = s.NameEn,
            NameAr = s.NameAr,
            CategoryEn = s.CategoryEn,
            CategoryAr = s.CategoryAr,
            Proficiency = s.Proficiency,
            IconUrl = s.IconUrl,
            DescriptionEn = s.DescriptionEn,
            DescriptionAr = s.DescriptionAr,
            Order = s.Order,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        public static Skill ToEntity(CreateSkillDto dto, int id = 0) => new()
        {
            Id = id,
            NameEn = dto.NameEn?.Trim() ?? string.Empty,
            NameAr = dto.NameAr?.Trim() ?? string.Empty,
            CategoryEn = dto.CategoryEn?.Trim() ?? string.Empty,
            CategoryAr = dto.CategoryAr?.Trim() ?? string.Empty,
            Proficiency = dto.Proficiency,
            IconUrl = dto.IconUrl,
            DescriptionEn = dto.DescriptionEn,
            DescriptionAr = dto.DescriptionAr,
            Order = dto.Order,
            IsActive = dto.IsActive
        };
    }
}
