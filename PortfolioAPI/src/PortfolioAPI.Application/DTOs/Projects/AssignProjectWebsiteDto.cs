namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Input model for assigning a project to a website.</summary>
    public class AssignProjectWebsiteDto
    {
        public int WebsiteId { get; set; }
        public bool IsPublished { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
