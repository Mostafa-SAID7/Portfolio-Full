using System;
using System.Linq;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Website entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static WebsiteDto ToDto(Website w) => new()
        {
            Id = w.Id,
            Name = w.Name,
            Slug = w.Slug,
            Description = w.Description,
            BaseUrl = w.BaseUrl,
            Order = w.Order,
            IsActive = w.IsActive,
            AssignedProjectsCount = w.ProjectWebsites?.Count(pw => pw.Project != null && !pw.Project.IsDeleted) ?? 0,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        };

        public static Website ToEntity(CreateWebsiteDto dto, int id = 0) => new()
        {
            Id = id,
            Name = dto.Name?.Trim() ?? string.Empty,
            Slug = dto.Slug?.Trim().ToLowerInvariant() ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim(),
            Order = dto.Order,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
