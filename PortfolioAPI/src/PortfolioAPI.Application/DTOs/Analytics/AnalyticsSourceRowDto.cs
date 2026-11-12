namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Traffic source / medium split.</summary>
    public class AnalyticsSourceRowDto
    {
        public string Source { get; set; } = string.Empty;
        public string Medium { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Users { get; set; }
    }
}
