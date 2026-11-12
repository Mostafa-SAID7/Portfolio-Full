namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Inclusive start/end date range for GA4 reporting.</summary>
    public class AnalyticsRangeDto
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int Days { get; set; }
    }
}
