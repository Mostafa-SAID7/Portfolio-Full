using System.Collections.Generic;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>
    /// Payload accepted when creating a project (POST api/Projects).
    /// Bound via DTO (never the raw EF entity) so server-managed fields
    /// cannot be over-posted.
    /// </summary>
    public class CreateProjectDto
    {
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
        public bool IsActive { get; set; } = true;

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
        public List<AssignProjectWebsiteDto>? WebsiteAssignments { get; set; }
        public List<AssignProjectWebsiteDto>? Websites
        {
            get => WebsiteAssignments;
            set => WebsiteAssignments = value;
        }
    }
}
