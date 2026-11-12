namespace PortfolioAPI.Application.DTOs
{
    /// <summary>A page and how often it was viewed.</summary>
    public class AnalyticsPageRowDto
    {
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int PageViews { get; set; }
        public int Users { get; set; }
    }
}
