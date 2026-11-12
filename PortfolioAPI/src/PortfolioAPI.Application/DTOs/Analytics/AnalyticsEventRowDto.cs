namespace PortfolioAPI.Application.DTOs
{
    /// <summary>A tracked event with its count.</summary>
    public class AnalyticsEventRowDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Users { get; set; }
    }
}
