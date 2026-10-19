using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>
    /// The single entity ⇄ DTO mapping owner for the API. Controllers
    /// (admin surface) and PublicController (portfolio site) share these, so
    /// a field added to an entity is mapped in exactly one place — the seven
    /// hand-copied <c>Map()</c> helpers this replaces (six in controllers, one
    /// in ProjectService, plus a verbatim 27-line clone in PublicController)
    /// were the largest DRY defect in the codebase.
    ///
    /// Mapping is explicit and null-tolerant: list properties materialize as
    /// empty lists (never null) so the JSON contract is stable.
    /// </summary>
    public static class EntityMappings
    {
        public static ProjectDto ToDto(Project p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            Technologies = p.Technologies?.ToList() ?? new List<string>(),
            GithubUrl = p.GithubUrl,
            LiveUrl = p.LiveUrl,
            Featured = p.Featured,
            Order = p.Order,
            IsActive = p.IsActive,
            Category = p.Category,
            Client = p.Client,
            Type = p.Type,
            Database = p.Database,
            Status = p.Status,
            Badges = p.Badges?.ToList() ?? new List<string>(),
            Metrics = p.Metrics?.Select(m => new ProjectMetricDto { Label = m.Label, Value = m.Value }).ToList()
                      ?? new List<ProjectMetricDto>(),
            Gradient = p.Gradient,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        public static SkillDto ToDto(Skill s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Category = s.Category,
            Proficiency = s.Proficiency,
            IconUrl = s.IconUrl,
            Description = s.Description,
            Order = s.Order,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        public static ExperienceDto ToDto(Experience e) => new()
        {
            Id = e.Id,
            Company = e.Company,
            Position = e.Position,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Description = e.Description,
            Technologies = e.Technologies?.ToList() ?? new List<string>(),
            Location = e.Location,
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

        // --- Payload → request-shaped entity (the route id wins) ---------------

        public static Skill ToEntity(CreateSkillDto dto, int id = 0) => new()
        {
            Id = id,
            Name = dto.Name?.Trim() ?? string.Empty,
            Category = dto.Category?.Trim() ?? string.Empty,
            Proficiency = dto.Proficiency,
            IconUrl = dto.IconUrl,
            Description = dto.Description,
            Order = dto.Order,
            IsActive = dto.IsActive
        };

        public static Experience ToEntity(CreateExperienceDto dto, int id = 0) => new()
        {
            Id = id,
            Company = dto.Company?.Trim() ?? string.Empty,
            Position = dto.Position?.Trim() ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description?.Trim() ?? string.Empty,
            Technologies = dto.Technologies ?? new List<string>(),
            Location = dto.Location,
            Current = dto.Current,
            Order = dto.Order,
            IsActive = dto.IsActive
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
            IsActive = dto.IsActive,
            ProjectUrl = dto.ProjectUrl
        };
    }
}
