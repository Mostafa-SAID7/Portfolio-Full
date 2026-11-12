namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Device category split (desktop / mobile / tablet).</summary>
    public class AnalyticsDeviceRowDto
    {
        public string Category { get; set; } = string.Empty;
        public int Users { get; set; }
        public double Share { get; set; }
    }
}
