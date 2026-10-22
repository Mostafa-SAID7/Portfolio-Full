using Microsoft.AspNetCore.RateLimiting;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Rate limiting — extracted from Program.cs.
    /// </summary>
    public static class RateLimitingConfiguration
    {
        /// <summary>Rate limiter name used by the public contact form.</summary>
        public const string PublicSubmitPolicy = "public-submit";

        /// <summary>Rate limiter name used by admin login.</summary>
        public const string LoginPolicy = "login";

        public static IServiceCollection AddPublicRateLimiting(this IServiceCollection services)
        {
            // --- Rate limiting (issue #10) ---------------------------------------
            // Per-client-IP fixed windows on the public contact form and on admin
            // login. ForwardedHeaders (see ProxyConfiguration) restores the real
            // client IP behind the Vercel proxy / hosting load balancer so
            // legitimate visitors don't share a bucket.
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { message = "Too many requests — please wait a moment and try again." },
                        cancellationToken);
                };

                options.AddFixedWindowLimiter(PublicSubmitPolicy, limiter =>
                {
                    limiter.PermitLimit = 10;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter(LoginPolicy, limiter =>
                {
                    limiter.PermitLimit = 5;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });
            });

            return services;
        }
    }
}
