using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Endpoints
{
    /// <summary>
    /// Minimal-API endpoints (health, version root, landing page) — extracted
    /// from Program.cs. Controllers remain the versioned JSON contract;
    /// these are process-liveness and discoverability helpers.
    /// </summary>
    public static class InfoEndpoints
    {
        public static IEndpointRouteBuilder MapInfoEndpoints(
            this IEndpointRouteBuilder endpoints,
            bool swaggerEnabled)
        {
            // Health endpoint: verifies process + database connectivity (used for
            // deployment checks and uptime monitors; 503 when the DB is unreachable).
            // Served under the versioned path (canonical, issue #24) plus the legacy
            // unversioned alias for existing monitors.
            endpoints.MapGet("/api/v1/health", HealthAsync);
            endpoints.MapGet("/api/health", HealthAsync); // deprecated alias (issue #24)

            // Version root: makes the Deprecation `Link` successor target resolve (it
            // used to point at /api/v1, which was a dead 404) and gives human consumers
            // a discoverable entry point.
            endpoints.MapGet("/api/v1", () => Results.Ok(new
            {
                name = "Portfolio API",
                version = "v1",
                health = "/api/v1/health",
                publicApi = "/api/v1/public/*",
                docs = "/swagger"
            }));

            // Root landing: never a dead 404 — send browsers to the Swagger UI (human
            // landing page for the API) or, when Swagger is disabled, a JSON index.
            endpoints.MapGet("/", () => swaggerEnabled
                ? Results.Redirect("/swagger")
                : Results.Ok(new
                {
                    name = "Portfolio API",
                    version = "v1",
                    health = "/api/v1/health",
                    publicApi = "/api/v1/public/*"
                }));

            return endpoints;
        }

        private static async Task<IResult> HealthAsync(
            PortfolioDbContext db,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            // Which instance is this? The dev API and the production API share ONE
            // database but not the upload folder, so a change made through the wrong
            // instance is invisible on the live site. The name travels with the health
            // payload so the dashboard can show it.
            var instance = configuration["Api:Instance"] ?? (configuration["ASPNETCORE_ENVIRONMENT"] ?? "unknown");
            try
            {
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? Results.Ok(new { status = "ok", database = "ok", instance, time = now })
                    : Results.Json(new { status = "degraded", database = "unreachable", instance, time = now },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                // Never swallow silently - uptime monitors see 503, the log says why.
                loggerFactory.CreateLogger("PortfolioAPI.Health")
                    .LogWarning(ex, "Health probe failed: database unreachable.");
                return Results.Json(new { status = "degraded", database = "unreachable", instance, time = now },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
