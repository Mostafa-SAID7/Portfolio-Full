using System;
using System.Collections.Generic;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Read model for a project (api/Projects, api/public/projects).</summary>
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? StorageProvider { get; set; }
        public string? StoragePublicId { get; set; }
        public List<string> Technologies { get; set; } = new();
        public string? GithubUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public string CategoryEn { get; set; } = string.Empty;
        public string CategoryAr { get; set; } = string.Empty;
        public string ClientEn { get; set; } = string.Empty;
        public string ClientAr { get; set; } = string.Empty;
        public string TypeEn { get; set; } = string.Empty;
        public string TypeAr { get; set; } = string.Empty;
        public string DatabaseEn { get; set; } = string.Empty;
        public string DatabaseAr { get; set; } = string.Empty;
        public string StatusEn { get; set; } = string.Empty;
        public string StatusAr { get; set; } = string.Empty;
        public List<string> BadgesEn { get; set; } = new();
        public List<string> BadgesAr { get; set; } = new();
        public List<ProjectMetricDto> MetricsEn { get; set; } = new();
        public List<ProjectMetricDto> MetricsAr { get; set; } = new();
        public string? Gradient { get; set; }
        public List<ProjectWebsiteDto> Websites { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
