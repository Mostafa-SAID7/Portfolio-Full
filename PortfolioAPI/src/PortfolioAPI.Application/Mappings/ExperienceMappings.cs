using System.Collections.Generic;
using System.Linq;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Experience entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static ExperienceDto ToDto(Experience e) => new()
        {
            Id = e.Id,
            CompanyEn = e.CompanyEn,
            CompanyAr = e.CompanyAr,
            PositionEn = e.PositionEn,
            PositionAr = e.PositionAr,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            DescriptionEn = e.DescriptionEn,
            DescriptionAr = e.DescriptionAr,
            Technologies = e.Technologies?.ToList() ?? new List<string>(),
            LocationEn = e.LocationEn,
            LocationAr = e.LocationAr,
            Current = e.Current,
            Order = e.Order,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };

        public static Experience ToEntity(CreateExperienceDto dto, int id = 0) => new()
        {
            Id = id,
            CompanyEn = dto.CompanyEn?.Trim() ?? string.Empty,
            CompanyAr = dto.CompanyAr?.Trim() ?? string.Empty,
            PositionEn = dto.PositionEn?.Trim() ?? string.Empty,
            PositionAr = dto.PositionAr?.Trim() ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DescriptionEn = dto.DescriptionEn?.Trim() ?? string.Empty,
            DescriptionAr = dto.DescriptionAr?.Trim() ?? string.Empty,
            Technologies = dto.Technologies ?? new List<string>(),
            LocationEn = dto.LocationEn,
            LocationAr = dto.LocationAr,
            Current = dto.Current,
            Order = dto.Order,
            IsActive = dto.IsActive
        };
    }
}
