using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Project entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static ProjectDto ToDto(Project p) => new()
        {
            Id = p.Id,
            Slug = p.Slug,
            TitleEn = p.TitleEn,
            TitleAr = p.TitleAr,
            DescriptionEn = p.DescriptionEn,
            DescriptionAr = p.DescriptionAr,
            ImageUrl = p.ImageUrl,
            StorageProvider = p.StorageProvider,
            StoragePublicId = p.StoragePublicId,
            Technologies = p.Technologies?.ToList() ?? new List<string>(),
            GithubUrl = p.GithubUrl,
            LiveUrl = p.LiveUrl,
            Featured = p.Featured,
            Order = p.Order,
            IsActive = p.IsActive,
            CategoryEn = p.CategoryEn,
            CategoryAr = p.CategoryAr,
            ClientEn = p.ClientEn,
            ClientAr = p.ClientAr,
            TypeEn = p.TypeEn,
            TypeAr = p.TypeAr,
            DatabaseEn = p.DatabaseEn,
            DatabaseAr = p.DatabaseAr,
            StatusEn = p.StatusEn,
            StatusAr = p.StatusAr,
            BadgesEn = p.BadgesEn?.ToList() ?? new List<string>(),
            BadgesAr = p.BadgesAr?.ToList() ?? new List<string>(),
            MetricsEn = p.MetricsEn?.Select(m => new ProjectMetricDto { LabelEn = m.LabelEn, LabelAr = m.LabelAr, Value = m.Value }).ToList()
                          ?? new List<ProjectMetricDto>(),
            MetricsAr = p.MetricsAr?.Select(m => new ProjectMetricDto { LabelEn = m.LabelEn, LabelAr = m.LabelAr, Value = m.Value }).ToList()
                          ?? new List<ProjectMetricDto>(),
            Gradient = p.Gradient,
            Websites = p.ProjectWebsites?.Where(pw => !pw.IsDeleted).Select(pw =>
            {
                var slug = !string.IsNullOrWhiteSpace(pw.Website?.Slug)
                    ? pw.Website.Slug
                    : (pw.WebsiteId == 1 ? "marketplace" :
                       pw.WebsiteId == 2 ? "banking" :
                       pw.WebsiteId == 3 ? "edutech" :
                       pw.WebsiteId == 4 ? "real-estate" :
                       pw.WebsiteId == 5 ? "healthcare" :
                       pw.WebsiteId == 6 ? "restaurants" :
                       pw.WebsiteId == 7 ? "platforms" :
                       pw.WebsiteId == 8 ? "entertainment" :
                       pw.WebsiteId == 9 ? "asp-net" : string.Empty);
                var name = !string.IsNullOrWhiteSpace(pw.Website?.Name)
                    ? pw.Website.Name
                    : (pw.WebsiteId == 1 ? "Marketplace" :
                       pw.WebsiteId == 2 ? "Fintech / Banking" :
                       pw.WebsiteId == 3 ? "EduTech" :
                       pw.WebsiteId == 4 ? "Real Estate" :
                       pw.WebsiteId == 5 ? "Healthcare" :
                       pw.WebsiteId == 6 ? "Restaurants" :
                       pw.WebsiteId == 7 ? "Platforms" :
                       pw.WebsiteId == 8 ? "Entertainment / Booking" :
                       pw.WebsiteId == 9 ? "ASP.NET Projects" : string.Empty);

                return new ProjectWebsiteDto
                {
                    WebsiteId = pw.WebsiteId,
                    WebsiteSlug = slug,
                    WebsiteName = name,
                    IsPublished = pw.IsPublished,
                    DisplayOrder = pw.DisplayOrder
                };
            }).ToList() ?? new List<ProjectWebsiteDto>(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        public static Project ToEntity(CreateProjectDto dto, int id = 0) => new()
        {
            Id = id,
            TitleEn = dto.TitleEn?.Trim() ?? string.Empty,
            TitleAr = dto.TitleAr?.Trim() ?? string.Empty,
            DescriptionEn = dto.DescriptionEn?.Trim() ?? string.Empty,
            DescriptionAr = dto.DescriptionAr?.Trim() ?? string.Empty,
            ImageUrl = dto.ImageUrl,
            StorageProvider = dto.StorageProvider,
            StoragePublicId = dto.StoragePublicId,
            Technologies = dto.Technologies ?? new List<string>(),
            GithubUrl = dto.GithubUrl,
            LiveUrl = dto.LiveUrl,
            Featured = dto.Featured,
            Order = dto.Order,
            IsActive = dto.IsActive,
            CategoryEn = dto.CategoryEn?.Trim() ?? string.Empty,
            CategoryAr = dto.CategoryAr?.Trim() ?? string.Empty,
            ClientEn = dto.ClientEn?.Trim() ?? string.Empty,
            ClientAr = dto.ClientAr?.Trim() ?? string.Empty,
            TypeEn = dto.TypeEn?.Trim() ?? string.Empty,
            TypeAr = dto.TypeAr?.Trim() ?? string.Empty,
            DatabaseEn = dto.DatabaseEn?.Trim() ?? string.Empty,
            DatabaseAr = dto.DatabaseAr?.Trim() ?? string.Empty,
            StatusEn = dto.StatusEn?.Trim() ?? string.Empty,
            StatusAr = dto.StatusAr?.Trim() ?? string.Empty,
            BadgesEn = dto.BadgesEn ?? new List<string>(),
            BadgesAr = dto.BadgesAr ?? new List<string>(),
            MetricsEn = dto.MetricsEn?.Select(m => new ProjectMetric
            {
                LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                Value = m.Value?.Trim() ?? string.Empty
            }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>(),
            MetricsAr = dto.MetricsAr?.Select(m => new ProjectMetric
            {
                LabelEn = m.LabelEn?.Trim() ?? string.Empty,
                LabelAr = m.LabelAr?.Trim() ?? string.Empty,
                Value = m.Value?.Trim() ?? string.Empty
            }).Where(m => m.LabelEn.Length > 0).ToList() ?? new List<ProjectMetric>(),
            Gradient = dto.Gradient,
            CreatedAt = DateTime.UtcNow
        };
    }
}
