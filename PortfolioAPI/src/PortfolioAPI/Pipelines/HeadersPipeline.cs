namespace PortfolioAPI.Pipelines
{
    /// <summary>
    /// Response-header middleware — extracted from Program.cs. Every extension
    /// here only sets headers; registration ORDER in Program.cs is the contract.
    /// </summary>
    public static class HeadersPipeline
    {
        /// <summary>
        /// Baseline security headers on every response (review finding: missing).
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                await next();
            });

            return app;
        }

        /// <summary>
        /// Cache-Control for the public content endpoints (issue #32). The output
        /// cache middleware keeps SQL out of the hot path; these headers let
        /// browsers and the Vercel edge reuse the response for the same TTL. Only
        /// anonymous GETs under the public prefix are affected - admin routes,
        /// auth and mutations never match, so they stay uncached.
        /// </summary>
        public static IApplicationBuilder UsePublicContentCacheHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var isPublicContent =
                    path.StartsWith("/api/v1/public/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/public/", StringComparison.OrdinalIgnoreCase);

                if (isPublicContent && HttpMethods.IsGet(context.Request.Method))
                {
                    context.Response.Headers.CacheControl = "public, max-age=30, stale-while-revalidate=60";
                }

                await next();
            });

            return app;
        }

        /// <summary>
        /// API versioning headers (issue #24): JSON endpoints answer under
        /// /api/v1/... (canonical) and on the legacy unversioned paths as
        /// deprecated aliases: responses advertise the version, and the aliases
        /// carry Deprecation/Link headers. Static file serving (/api/files/...)
        /// is intentionally exempt — those URLs are immutable content stored in
        /// the database, not a versioned contract.
        /// </summary>
        public static IApplicationBuilder UseApiVersionHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? "";
                var isJsonApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/api/files/", StringComparison.OrdinalIgnoreCase);
                if (isJsonApi)
                {
                    context.Response.Headers["X-API-Version"] = "v1";
                    if (!path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Headers["Deprecation"] = "true";
                        context.Response.Headers["Link"] = "</api/v1>; rel=\"successor-version\"";
                    }
                }
                await next();
            });

            return app;
        }
    }
}
