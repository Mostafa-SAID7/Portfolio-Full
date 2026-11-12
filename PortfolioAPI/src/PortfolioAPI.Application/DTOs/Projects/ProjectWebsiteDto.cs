namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Project-to-Website assignment read model.</summary>
    public class ProjectWebsiteDto
    {
        public int WebsiteId { get; set; }
        public string WebsiteSlug { get; set; } = string.Empty;
        public string WebsiteName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int DisplayOrder { get; set; }
    }
}
