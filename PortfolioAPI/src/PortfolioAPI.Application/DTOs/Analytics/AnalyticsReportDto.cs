using System;
using System.Collections.Generic;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Full analytics payload rendered by the dashboard.</summary>
    public class AnalyticsReportDto
    {
        public AnalyticsOverviewDto Overview { get; set; } = new();
        public List<AnalyticsTrendPointDto> Trend { get; set; } = new();
        public List<AnalyticsGeoRowDto> Countries { get; set; } = new();
        public List<AnalyticsSourceRowDto> Sources { get; set; } = new();
        public List<AnalyticsPageRowDto> Pages { get; set; } = new();
        public List<AnalyticsDeviceRowDto> Devices { get; set; } = new();
        public List<AnalyticsEventRowDto> Events { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public bool NotConfigured { get; set; }
    }
}
