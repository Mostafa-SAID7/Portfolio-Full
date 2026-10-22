using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Analytics wiring — the GA4 Data API client used by the private
    /// analytics dashboard.
    ///
    /// The service account credentials are read from the "Analytics" section
    /// or from the GA4_PROPERTY_ID / GA4_SERVICE_ACCOUNT_EMAIL /
    /// GA4_PRIVATE_KEY environment variables (the deployment-friendly form).
    /// Nothing is logged here: a secret value must never reach the logs.
    /// </summary>
    public static class AnalyticsConfiguration
    {
        public static IServiceCollection AddAnalytics(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AnalyticsOptions>(configuration.GetSection("Analytics"));

            // Environment overlay – the same values the dashboard's own
            // deployment uses. An explicit configuration value always wins.
            services.PostConfigure<AnalyticsOptions>(options =>
            {
                options.PropertyId = Prefer(options.PropertyId, "GA4_PROPERTY_ID");
                options.ServiceAccountEmail = Prefer(options.ServiceAccountEmail, "GA4_SERVICE_ACCOUNT_EMAIL");
                options.PrivateKey = Prefer(options.PrivateKey, "GA4_PRIVATE_KEY");
            });

            services.AddHttpClient("ga4", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PortfolioAPI/1.0");
            });

            services.AddSingleton<IAnalyticsReportingService, AnalyticsReportingService>();

            return services;
        }

        private static string Prefer(string configured, string environmentVariable) =>
            string.IsNullOrWhiteSpace(configured)
                ? Environment.GetEnvironmentVariable(environmentVariable) ?? string.Empty
                : configured;
    }
}
