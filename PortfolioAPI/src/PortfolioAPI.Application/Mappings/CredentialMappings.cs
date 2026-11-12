using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Credential entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static CredentialDto ToDto(Credential c) => new()
        {
            Id = c.Id,
            Tab = c.Tab,
            Year = c.Year,
            PeriodEn = c.PeriodEn,
            PeriodAr = c.PeriodAr,
            TitleEn = c.TitleEn,
            TitleAr = c.TitleAr,
            OrgEn = c.OrgEn,
            OrgAr = c.OrgAr,
            LocationEn = c.LocationEn,
            LocationAr = c.LocationAr,
            SummaryEn = c.SummaryEn,
            SummaryAr = c.SummaryAr,
            HighlightsEn = c.HighlightsEn?.ToList() ?? new List<string>(),
            HighlightsAr = c.HighlightsAr?.ToList() ?? new List<string>(),
            Stack = c.Stack?.ToList() ?? new List<string>(),
            CredentialId = c.CredentialId,
            StatusEn = c.StatusEn,
            StatusAr = c.StatusAr,
            Featured = c.Featured,
            Order = c.Order,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        public static Credential ToEntity(CreateCredentialDto dto, int id = 0) => new()
        {
            Id = id,
            Tab = dto.Tab?.Trim() ?? "work",
            Year = dto.Year?.Trim() ?? string.Empty,
            PeriodEn = dto.PeriodEn?.Trim() ?? string.Empty,
            PeriodAr = dto.PeriodAr?.Trim() ?? string.Empty,
            TitleEn = dto.TitleEn?.Trim() ?? string.Empty,
            TitleAr = dto.TitleAr?.Trim() ?? string.Empty,
            OrgEn = dto.OrgEn?.Trim() ?? string.Empty,
            OrgAr = dto.OrgAr?.Trim() ?? string.Empty,
            LocationEn = dto.LocationEn?.Trim() ?? string.Empty,
            LocationAr = dto.LocationAr?.Trim() ?? string.Empty,
            SummaryEn = dto.SummaryEn?.Trim() ?? string.Empty,
            SummaryAr = dto.SummaryAr?.Trim() ?? string.Empty,
            HighlightsEn = dto.HighlightsEn ?? new List<string>(),
            HighlightsAr = dto.HighlightsAr ?? new List<string>(),
            Stack = dto.Stack ?? new List<string>(),
            CredentialId = dto.CredentialId,
            StatusEn = dto.StatusEn,
            StatusAr = dto.StatusAr,
            Featured = dto.Featured,
            Order = dto.Order,
            IsActive = dto.IsActive
        };
    }
}
