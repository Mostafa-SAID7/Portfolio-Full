namespace PortfolioAPI.Core.DTOs
{
    /// <summary>
    /// Read models for the GA4 reports the private dashboard renders.
    /// Every DTO carries only aggregated numbers – no visitor identifiers,
    /// no IP addresses, no free text.
    /// </summary>
    public class AnalyticsRangeDto
    {
        /// <summary>Inclusive start date (yyyy-MM-dd).</summary>
        public string StartDate { get; set; } = string.Empty;
        /// <summary>Inclusive end date (yyyy-MM-dd).</summary>
        public string EndDate { get; set; } = string.Empty;
        /// <summary>Days covered by the range.</summary>
        public int Days { get; set; }
    }

    /// <summary>Headline numbers of the selected period.</summary>
    public class AnalyticsOverviewDto
    {
        public AnalyticsRangeDto Range { get; set; } = new();
        public int ActiveUsers { get; set; }
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
        public int EngagedSessions { get; set; }
        /// <summary>Engaged sessions / sessions, as a percentage (0-100).</summary>
        public double EngagementRate { get; set; }
        /// <summary>Average engagement time per session, in seconds.</summary>
        public double AverageEngagementSeconds { get; set; }
        /// <summary>Contact form submissions recorded by GA4.</summary>
        public int ContactConversions { get; set; }
        /// <summary>Previous period, for the "change vs. before" figures.</summary>
        public AnalyticsOverviewDto Previous { get; set; } = new();
        /// <summary>When the report was produced (UTC).</summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>One day of the trend line.</summary>
    public class AnalyticsTrendPointDto
    {
        public string Date { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
    }

    /// <summary>Users grouped by country (GA4 estimates, never exact).</summary>
    public class AnalyticsGeoRowDto
    {
        public string Country { get; set; } = string.Empty;
        public int Users { get; set; }
        public double Share { get; set; }
    }

    /// <summary>Traffic source / medium split.</summary>
    public class AnalyticsSourceRowDto
    {
        public string Source { get; set; } = string.Empty;
        public string Medium { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Users { get; set; }
    }

    /// <summary>A page and how often it was seen.</summary>
    public class AnalyticsPageRowDto
    {
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int PageViews { get; set; }
        public int Users { get; set; }
    }

    /// <summary>Device category split (desktop / mobile / tablet).</summary>
    public class AnalyticsDeviceRowDto
    {
        public string Category { get; set; } = string.Empty;
        public int Users { get; set; }
        public double Share { get; set; }
    }

    /// <summary>A tracked event with its count.</summary>
    public class AnalyticsEventRowDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Users { get; set; }
    }

    /// <summary>The full analytics payload the dashboard renders in one request.</summary>
    public class AnalyticsReportDto
    {
        public AnalyticsOverviewDto Overview { get; set; } = new();
        public List<AnalyticsTrendPointDto> Trend { get; set; } = new();
        public List<AnalyticsGeoRowDto> Countries { get; set; } = new();
        public List<AnalyticsSourceRowDto> Sources { get; set; } = new();
        public List<AnalyticsPageRowDto> Pages { get; set; } = new();
        public List<AnalyticsDeviceRowDto> Devices { get; set; } = new();
        public List<AnalyticsEventRowDto> Events { get; set; } = new();
        /// <summary>When the data was last refreshed (UTC) – the freshness policy.</summary>
        public DateTime GeneratedAt { get; set; }
        /// <summary>True when GA4 is not configured for this deployment.</summary>
        public bool NotConfigured { get; set; }
    }
}
