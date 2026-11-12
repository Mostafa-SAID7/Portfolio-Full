namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Users grouped by country.</summary>
    public class AnalyticsGeoRowDto
    {
        public string Country { get; set; } = string.Empty;
        public int Users { get; set; }
        public double Share { get; set; }
    }
}
