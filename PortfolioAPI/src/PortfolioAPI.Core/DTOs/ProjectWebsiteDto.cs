using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    public class ProjectWebsiteDto
    {
        public int WebsiteId { get; set; }
        public string WebsiteSlug { get; set; } = string.Empty;
        public string WebsiteName { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class AssignProjectWebsiteDto
    {
        public int WebsiteId { get; set; }
        public bool IsPublished { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateProjectWebsitesDto
    {
        public List<AssignProjectWebsiteDto> Websites { get; set; } = new();
    }
}

