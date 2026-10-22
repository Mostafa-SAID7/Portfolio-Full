using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>
    /// Centralised entity <-> DTO mapping.
    ///
    /// Mapping is explicit and null-tolerant: list properties materialize as
    /// empty lists (never null) so the JSON contract is stable.
    /// </summary>
    public static class EntityMappings
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
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

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

        public static ContactDto ToDto(Contact c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Subject = c.Subject,
            Message = c.Message,
            IsRead = c.IsRead,
            ReadAt = c.ReadAt,
            IpAddress = c.IpAddress,
            UserAgent = c.UserAgent,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        public static ContactReplyDto ToDto(ContactReply r) => new()
        {
            Id = r.Id,
            ContactId = r.ContactId,
            RecipientEmail = r.RecipientEmail,
            Subject = r.Subject,
            Body = r.Body,
            SentAt = r.SentAt
        };

        public static TestimonialDto ToDto(Testimonial t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Position = t.Position,
            Company = t.Company,
            Content = t.Content,
            ImageUrl = t.ImageUrl,
            Rating = t.Rating,
            Featured = t.Featured,
            Order = t.Order,
            IsActive = t.IsActive,
            ProjectUrl = t.ProjectUrl,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

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

        // --- Payload -> request-shaped entity (the route id wins) ---------------

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

        public static CategoryDto ToDto(Category c) => new()
        {
            Id = c.Id,
            NameEn = c.NameEn,
            NameAr = c.NameAr,
            Order = c.Order,
            IsActive = c.IsActive
        };

        public static Category ToEntity(CreateCategoryDto dto, int id = 0) => new()
        {
            Id = id,
            NameEn = dto.NameEn?.Trim() ?? string.Empty,
            NameAr = dto.NameAr?.Trim() ?? string.Empty,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        public static Testimonial ToEntity(CreateTestimonialDto dto, int id = 0) => new()
        {
            Id = id,
            Name = dto.Name?.Trim() ?? string.Empty,
            Position = dto.Position?.Trim() ?? string.Empty,
            Company = dto.Company?.Trim() ?? string.Empty,
            Content = dto.Content?.Trim() ?? string.Empty,
            ImageUrl = dto.ImageUrl,
            Rating = dto.Rating,
            Featured = dto.Featured,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        public static Credential ToEntity(CreateCredentialDto dto, int id = 0) => new()
        {
            Id = id,
            Tab = dto.Tab?.Trim() ?? "work",
            Year = dto.Year,
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
