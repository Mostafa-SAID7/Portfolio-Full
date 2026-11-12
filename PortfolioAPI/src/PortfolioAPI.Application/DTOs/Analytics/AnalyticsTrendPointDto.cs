namespace PortfolioAPI.Application.DTOs
{
    /// <summary>One day of the trend line.</summary>
    public class AnalyticsTrendPointDto
    {
        public string Date { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
    }
}
