using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>A bilingual label/value stat displayed on a project card.</summary>
    public class ProjectMetricDto
    {
        public string LabelEn { get; set; } = string.Empty;
        public string LabelAr { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Read model for a bilingual project (api/Projects, api/public/projects).</summary>
    public class ProjectDto
    {
        public int Id { get; set; }

        /// <summary>Stable URL identifier - never changes when the title does.</summary>
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

        // Editorial fields consumed by the public portfolio.
        public string? CategoryEn { get; set; }
        public string? CategoryAr { get; set; }
        public string? ClientEn { get; set; }
        public string? ClientAr { get; set; }
        public string? TypeEn { get; set; }
        public string? TypeAr { get; set; }
        public string? DatabaseEn { get; set; }
        public string? DatabaseAr { get; set; }
        public string? StatusEn { get; set; }
        public string? StatusAr { get; set; }
        public List<string> BadgesEn { get; set; } = new();
        public List<string> BadgesAr { get; set; } = new();
        public List<ProjectMetricDto> MetricsEn { get; set; } = new();
        public List<ProjectMetricDto> MetricsAr { get; set; } = new();
        public string? Gradient { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
