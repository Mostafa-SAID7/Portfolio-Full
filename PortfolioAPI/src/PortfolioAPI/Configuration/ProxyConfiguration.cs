using Microsoft.AspNetCore.Builder;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Reverse-proxy wiring — extracted from Program.cs.
    /// </summary>
    public static class ProxyConfiguration
    {
        public static IServiceCollection AddForwardedClientIp(this IServiceCollection services)
        {
            // Real client IP + scheme behind proxies (X-Forwarded-For /
            // X-Forwarded-Proto). Deliberately clearing KnownProxies/KnownNetworks:
            // behind a single appending proxy the middleware then identifies the
            // client from the NEAREST forwarded entry, which the proxy itself
            // appended — clients cannot mint rate limit buckets by forging
            // X-Forwarded-For. This is only safe while clients cannot reach
            // Kestrel directly; if you ever expose the port publicly, populate
            // KnownProxies/KnownNetworks instead of clearing them.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
        }
    }
}
