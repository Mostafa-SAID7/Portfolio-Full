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

        // Navigation properties
        public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    }
}