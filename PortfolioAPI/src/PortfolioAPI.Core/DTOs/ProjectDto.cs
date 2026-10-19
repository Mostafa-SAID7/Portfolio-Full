using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>A label/value stat displayed on a project card.</summary>
    public class ProjectMetricDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Read model for a project (api/Projects, api/public/projects).</summary>
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Technologies { get; set; } = new();
        public string? GithubUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }

        // Editorial fields consumed by the public portfolio.
        public string? Category { get; set; }
        public string? Client { get; set; }
        public string? Type { get; set; }
        public string? Database { get; set; }
        public string? Status { get; set; }
        public List<string> Badges { get; set; } = new();
        public List<ProjectMetricDto> Metrics { get; set; } = new();
        public string? Gradient { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
