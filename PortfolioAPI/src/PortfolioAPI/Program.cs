using PortfolioAPI.Commands;
using PortfolioAPI.Configuration;
using PortfolioAPI.Endpoints;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Pipelines;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — assembly point only.
//
// Every configuration concern lives in its own static class under one of:
//   Configuration/   → IServiceCollection registrations, one related group per file
//   Pipelines/       → IApplicationBuilder middleware, ordered exactly as used below
//   Endpoints/       → minimal-API endpoints (health, version root, landing)
//   Commands/        → CLI commands (migrate-media)
//
// Read this file top-to-bottom and you see the FULL startup contract:
// registrations first, then the request pipeline in execution order.
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Swagger UI (issue #3/#8): OFF by default outside Development; every
// environment can still opt in explicitly with `Swagger__Enabled=true`
// (the production config does so deliberately). Docs also advertise the
// JWT bearer scheme while auth is enabled.
bool swaggerEnabled = SwaggerConfiguration.IsEnabled(builder.Configuration, builder.Environment);
if (swaggerEnabled)
{
    builder.Services.AddSwaggerDocs();
}

// Add services to the container — one call per related concern.
builder.WebHost.ConfigureKestrelLimits();                    // Kestrel body limit (10 MB)
builder.Services
    .AddApiControllers()                                     // controllers + logging
    .AddPublicContentCaching()                               // output cache (public content, issue #32)
    .AddJsonCompression()                                    // gzip JSON responses
    .AddFrontendCors(builder.Configuration, builder.Environment) // CORS (issue #24 exposed headers)
    .AddAdminAuthentication(builder.Configuration)           // JWT bearer (issues #1/#6)
    .AddPublicRateLimiting()                                 // contact + login windows (issue #10)
    .AddForwardedClientIp()                                  // real client IP behind proxies
    .AddDatabase(builder.Configuration)                      // SQL Server + UnitOfWork (fail-fast)
    .AddApplicationServices(builder.Configuration)           // domain services + email options + FluentValidation
    .AddStorage(builder.Configuration, builder.Environment); // local disk OR Cloudinary (fail-fast)

// Backend-owned upload storage: anchor App_Data to the content root so the
// bound FileStorageOptions and the static-file middleware below agree
// (see StorageConfiguration for the reasoning).
StorageConfiguration.AnchorUploadPath(builder.Configuration, builder.Environment);

var app = builder.Build();

// --- Media migration command -------------------------------------------------
// `dotnet run --project PortfolioAPI -- migrate-media [--dry-run]`
if (MediaMigrationCommand.IsRequested(args))
{
    Environment.ExitCode = await MediaMigrationCommand.ExecuteAsync(app, args);
    return;
}

// ─────────────────────────────────────────────────────────────────────────────
// Request pipeline — the ORDER below is the contract; do not re-arrange.
// ─────────────────────────────────────────────────────────────────────────────

// Restore real client IP/scheme behind the hosting reverse proxy — first,
// since everything downstream (rate limiting, redirects) reads them.
app.UseForwardedHeaders();

// Error handling wraps everything below so CORS, static uploads, Swagger,
// controllers all share the JSON error shape (see ErrorHandlingPipeline).
app.UseApiErrorHandling();

// Transport security before any response can leave (Swagger UI included).
// HSTS is a no-op until TLS exists (#19) and only ever applies to https://
// responses, so it is safe to enable unconditionally outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Use HTTPS redirection (only when an HTTPS port is configured)
app.UseHttpsRedirection();

// Baseline security headers on every response (review finding: missing).
app.UseSecurityHeaders();

// Use CORS — MUST sit before UseResponseCompression and UseOutputCache so
// cross-origin headers are attached to every response, including cached
// public content.
app.UseCors(CorsConfiguration.PolicyName);

// Compress JSON responses (gzip) — must sit before the endpoints write.
app.UseResponseCompression();

// Output cache (public content only - see AddPublicContentCaching). Must run
// before the endpoints so cached responses skip the action entirely.
app.UseOutputCache();

// Cache-Control for the public content endpoints (issue #32).
app.UsePublicContentCacheHeaders();

// Serve uploaded files (UploadController) at {FileStorage:BaseUrl} — /api/files
// by default. Both frontends reach this path through their own /api proxy, so
// stored cover URLs stay relative in the database and load over HTTPS on every
// frontend without any hardcoded API host.
app.UseLocalUploads();

// Swagger UI: /swagger  (JSON spec: /swagger/v1/swagger.json)
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio API v1");
        options.DocumentTitle = "Portfolio API";
    });
}

// Health, version root and landing endpoints (minimal API).
app.MapInfoEndpoints(swaggerEnabled);

// API versioning headers (issue #24): X-API-Version + Deprecation/Link aliases.
app.UseApiVersionHeaders();

// Rate limiting (public contact + login policies) and JWT authentication
// must run before the endpoints; /api/v1/public/* endpoints carry no
// [Authorize] and stay anonymous.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Initialize/migrate the database.
app.Logger.LogInformation("Starting database initialization...");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PortfolioDbContext>();
        await DbInitializer.InitializeAsync(context, app.Logger);
        app.Logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database. Continuing without database initialization...");
        // Don't fail fast in production - let the app start and handle DB errors per-request
        // This prevents 500.30 on startup while still logging the error
    }
}

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> for the
// integration test project (top-level statements generate an internal class).
public partial class Program { }
