using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    public class Project : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Technologies { get; set; } = new();
        public string? GithubUrl { get; set; }
        public string? LiveUrl { get; set; }
        public bool Featured { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Editorial/presentation fields consumed by the public portfolio
        // (filter chips, client badge, gradient fallback art, metrics strip).
        public string? Category { get; set; }
        public string? Client { get; set; }
        public string? Type { get; set; }          // Team | Freelance | Self
        public string? Database { get; set; }
        public string? Status { get; set; }        // In Progress | Completed | Deployed
        // Nullable: the columns are nullable (ALTERed in on existing databases)
        // and EF bypasses value converters when the stored value is NULL.
        public List<string>? Badges { get; set; } = new();
        public List<ProjectMetric>? Metrics { get; set; } = new();
        public string? Gradient { get; set; }

        // Navigation properties
        public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    }
}