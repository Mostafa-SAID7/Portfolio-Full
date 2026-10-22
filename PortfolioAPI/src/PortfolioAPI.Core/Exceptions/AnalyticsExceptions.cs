namespace PortfolioAPI.Core.Exceptions
{
    /// <summary>
    /// GA4 is not configured for this deployment (no property id or no
    /// service account). The dashboard shows an actionable "not set up" state
    /// instead of pretending there is no traffic.
    /// </summary>
    public class AnalyticsNotConfiguredException : Exception
    {
        public AnalyticsNotConfiguredException(string message) : base(message) { }
    }

    /// <summary>
    /// GA4 is configured but the report could not be produced (quota, network,
    /// permission). The dashboard shows the failure and the last refresh time.
    /// </summary>
    public class AnalyticsUnavailableException : Exception
    {
        public AnalyticsUnavailableException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
