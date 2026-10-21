namespace PortfolioAPI.Infrastructure.Emailing
{
    /// <summary>
    /// Strongly-typed `EmailSettings` section (same Options pattern the storage
    /// settings use) — bound once in Configuration, injected here; EmailService
    /// no longer reads per-call magic strings out of IConfiguration.
    /// `AdminEmail` may live here or fall back to the `Auth:Email` profile
    /// address (that fallback lives in EmailService because it crosses sections).
    /// </summary>
    public sealed class EmailSettingsOptions
    {
        public const string SectionName = "EmailSettings";

        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Portfolio API";
        public string AdminEmail { get; set; } = string.Empty;

        /// <summary>True for the placeholder values shipped in appsettings.json.</summary>
        public static bool IsPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value)
            || value.StartsWith("your-", StringComparison.OrdinalIgnoreCase);
    }
}
