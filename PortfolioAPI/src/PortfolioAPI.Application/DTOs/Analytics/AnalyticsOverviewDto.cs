using System;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Headline aggregate figures of the selected period.</summary>
    public class AnalyticsOverviewDto
    {
        public AnalyticsRangeDto Range { get; set; } = new();
        public int ActiveUsers { get; set; }
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
        public int EngagedSessions { get; set; }
        public double EngagementRate { get; set; }
        public double AverageEngagementSeconds { get; set; }
        public int ContactConversions { get; set; }
        public AnalyticsOverviewDto Previous { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
