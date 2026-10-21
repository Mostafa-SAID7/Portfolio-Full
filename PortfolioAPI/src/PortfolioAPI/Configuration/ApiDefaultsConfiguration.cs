using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// HTTP host defaults: controllers, logging, the Kestrel body limit, output
    /// caching for public content and response compression — extracted from
    /// Program.cs so each concern stays reviewable on its own.
    /// </summary>
    public static class ApiDefaultsConfiguration
    {
        /// <summary>Shared name of the public-content output-cache policy + tag.</summary>
        public const string PublicCachePolicy = "public-content";
        public const string PublicCacheTag = "public";

        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            // Add controllers
            services.AddControllers();

            // Add logging
            services.AddLogging();

            return services;
        }

        /// <summary>
        /// Increase startup time limit for IIS/ANCM (default 120s) —
        /// DbInitializer may need more time on cold start.
        /// </summary>
        public static IWebHostBuilder ConfigureKestrelLimits(this IWebHostBuilder host)
        {
            host.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 10485760; // 10MB
            });
            return host;
        }

        /// <summary>
        /// Output caching for the public, read-only content endpoints (issue #32).
        /// Public portfolio content changes only when the dashboard saves, so a
        /// short TTL keeps SQL out of the hot path without any invalidation plumbing:
        /// a dashboard edit becomes visible at most one TTL later. Admin routes are
        /// never cached (the attribute is applied per action on PublicController).
        /// </summary>
        public static IServiceCollection AddPublicContentCaching(this IServiceCollection services)
        {
            services.AddOutputCache(options =>
            {
                options.AddPolicy(PublicCachePolicy, policy =>
                    policy
                        .Expire(TimeSpan.FromSeconds(30))
                        .SetVaryByQuery("featured")
                        .SetVaryByHeader("Origin")
                        .Tag(PublicCacheTag));
            });

            return services;
        }

        /// <summary>
        /// Response compression: the JSON payloads (projects/skills lists) are
        /// 8-9 KB uncompressed; gzip cuts them to ~1 KB, which matters a lot on
        /// the Vercel → MonsterASP hop and on mobile connections.
        /// </summary>
        public static IServiceCollection AddJsonCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/json",
                    "application/problem+json",
                    "application/octet-stream"
                });
            });

            return services;
        }
    }
}
