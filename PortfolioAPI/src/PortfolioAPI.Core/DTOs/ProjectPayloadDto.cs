using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Payload accepted when creating a project.</summary>
    public class CreateProjectDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Technologies { get; set; } = new();
        public string? GithubUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;

        // Editorial fields consumed by the public portfolio.
        public string? Category { get; set; }
        public string? Client { get; set; }
        public string? Type { get; set; }
        public string? Database { get; set; }
        public string? Status { get; set; }
        public List<string> Badges { get; set; } = new();
        public List<ProjectMetricDto> Metrics { get; set; } = new();
        public string? Gradient { get; set; }
    }

    /// <summary>Payload accepted when updating a project.</summary>
    public class UpdateProjectDto : CreateProjectDto
    {
    }
}
