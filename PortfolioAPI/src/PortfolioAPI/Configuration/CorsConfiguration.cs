using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// CORS wiring — extracted from Program.cs. Development allows any origin
    /// (local Vite/Angular dev servers); every other environment only accepts
    /// the origins configured in `Cors:AllowedOrigins` (env:
    /// `Cors__AllowedOrigins`, comma-separated, e.g.
    /// "https://my-site.vercel.app,https://*.vercel.app" — a single "*"
    /// wildcard is allowed inside an origin pattern).
    /// </summary>
    public static class CorsConfiguration
    {
        /// <summary>
        /// The single named policy every controller pipeline uses. Must sit
        /// before UseResponseCompression and UseOutputCache so cross-origin
        /// headers are attached to every response, including cached content.
        /// </summary>
        public const string PolicyName = "Frontends";

        public static IServiceCollection AddFrontendCors(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          // Expose the versioning signals (issue #24) so browser devtools
                          // on the frontends can read them cross-origin.
                          .WithExposedHeaders("X-API-Version", "Deprecation");

                    if (environment.IsDevelopment())
                    {
                        policy.AllowAnyOrigin();
                    }
                    else
                    {
                        var allowedOrigins = ParseOrigins(configuration);

                        if (allowedOrigins.Length > 0)
                        {
                            var (exactOrigins, originRegexes) = CompileOriginMatchers(allowedOrigins);

                            // Exact origins are compared directly; patterns containing '*'
                            // (e.g. https://*.vercel.app) are matched against the compiled set.
                            policy.SetIsOriginAllowed(origin =>
                                exactOrigins.Contains(origin) || originRegexes.Any(r => r.IsMatch(origin)));
                        }
                        else
                        {
                            // With no origins configured, cross-origin requests are
                            // denied and only same-origin callers work.
                            policy.SetIsOriginAllowed(_ => false);
                        }
                    }
                });
            });

            return services;
        }

        private static string[] ParseOrigins(IConfiguration configuration) =>
            (configuration["Cors:AllowedOrigins"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>
        /// Origin patterns are precompiled ONCE at startup: the previous version
        /// built and compiled a fresh regex per request on this hot path.
        /// </summary>
        internal static (HashSet<string> Exact, Regex[] Patterns) CompileOriginMatchers(string[] allowedOrigins)
        {
            var exactOrigins = new HashSet<string>(
                allowedOrigins.Where(pattern => !pattern.Contains('*')),
                StringComparer.OrdinalIgnoreCase);

            var originRegexes = allowedOrigins
                .Where(pattern => pattern.Contains('*'))
                .Select(pattern => new Regex(
                    "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                    RegexOptions.IgnoreCase
                    | RegexOptions.CultureInvariant
                    | RegexOptions.Compiled))
                .ToArray();

            return (exactOrigins, originRegexes);
        }
    }
}
