namespace PortfolioAPI.Core.Interfaces.Services
{
    using PortfolioAPI.Core.DTOs;

    /// <summary>
    /// Reads the GA4 reports that power the private analytics dashboard.
    ///
    /// Implemented in the infrastructure layer with the official GA4 Data API
    /// (v1beta). The Google credentials never leave the server: the dashboard
    /// only ever receives aggregated DTOs.
    /// </summary>
    public interface IAnalyticsReportingService
    {
        /// <summary>True when a GA4 property and service account are configured.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// The whole dashboard payload for a period (inclusive start/end date).
        /// Throws <c>AnalyticsNotConfiguredException</c> when GA4 is not set up
        /// and <c>AnalyticsUnavailableException</c> when the API cannot be read.
        /// </summary>
        Task<AnalyticsReportDto> GetReportAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);
    }
}
