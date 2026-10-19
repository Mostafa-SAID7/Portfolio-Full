using PortfolioAPI.Application.Services;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using PortfolioAPI.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Swagger UI is enabled in every environment by default (the API currently
// exposes no authenticated surface, so published docs add no extra risk).
// Disable it anywhere with `Swagger__Enabled=false` (appsettings or env var).
var swaggerEnabled = builder.Configuration.GetValue("Swagger:Enabled", true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

if (swaggerEnabled)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Portfolio API",
            Version = "v1",
            Description = "Centralized backend for the Portfolio admin dashboard and the public portfolio site. "
                          + "Admin endpoints live under /api/*; the public, read-only surface used by the portfolio "
                          + "frontend lives under /api/public/*."
        });
    });
}

// Add controllers
builder.Services.AddControllers();

// CORS: Development allows any origin (local Vite/Angular dev servers);
// every other environment only accepts the origins configured in
// `Cors:AllowedOrigins` (env: `Cors__AllowedOrigins`, comma-separated,
// e.g. "https://my-site.vercel.app,https://*.vercel.app" — a single "*"
// wildcard is allowed inside an origin pattern). With no origins configured,
// cross-origin requests are denied and only same-origin callers work.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader();

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin();
        }
        else if (allowedOrigins.Length > 0)
        {
            // Exact origins are compared directly; patterns containing '*'
            // (e.g. https://*.vercel.app) are matched as wildcards.
            policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Any(pattern =>
                    pattern.Contains('*')
                        ? System.Text.RegularExpressions.Regex.IsMatch(
                            origin,
                            "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                        : string.Equals(pattern, origin, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Add logging
builder.Services.AddLogging();

// Connection string: provided by the environment in production
// (`ConnectionStrings__DefaultConnection`) and by the untracked
// appsettings.Development.json (or user-secrets) locally. Fail fast with a
// clear message instead of a provider error on the first request.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. Set the ConnectionStrings__DefaultConnection " +
        "environment variable (production) or appsettings.Development.json / user-secrets (local development).");
}

// Add database context (SQL Server from ConnectionStrings:DefaultConnection)
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure()));

// Add repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IExperienceService, ExperienceService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();
builder.Services.AddScoped<IBlogService, BlogService>();

builder.Services.AddScoped<ICredentialService, CredentialService>();

// Add infrastructure services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ICacheService, CacheService>();

// Distributed cache backing the CacheService. Swap for Redis by adding a
// Redis connection string and calling AddStackExchangeRedisCache instead.
builder.Services.AddDistributedMemoryCache();

// Add AutoMapper (profiles and validators live in the Application assembly)
builder.Services.AddAutoMapper(typeof(PortfolioAPI.Application.Mappings.AutoMapperProfile).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(PortfolioAPI.Application.Services.ProjectService).Assembly);

// Backend-owned upload storage (project covers etc.): keep it OUTSIDE
// wwwroot (App_Data) so redeploying the publish output can never clobber
// uploaded files, and anchor it to the content root so it works the same
// under `dotnet run` and IIS/ANCM hosting.
var configuredStoragePath = builder.Configuration["FileStorage:Path"];
builder.Configuration["FileStorage:Path"] =
    string.IsNullOrWhiteSpace(configuredStoragePath)
        ? Path.Combine(builder.Environment.ContentRootPath, UploadStorage.DefaultRelativePath)
        : Path.IsPathRooted(configuredStoragePath)
            ? configuredStoragePath
            : Path.Combine(builder.Environment.ContentRootPath, configuredStoragePath);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use CORS (policy name set up above)
app.UseCors("Frontends");

// Serve uploaded files (UploadController) at {FileStorage:BaseUrl} — /api/files
// by default. Both frontends reach this path through their own /api proxy, so
// stored cover URLs stay relative in the database and load over HTTPS on every
// frontend without any hardcoded API host.
var uploadsRoot = UploadStorage.ResolveRoot(app.Configuration);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = UploadStorage.PublicBase(app.Configuration),
    // Uploads use random (guid) filenames, so they are immutable — cache hard.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
});

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

// Use HTTPS redirection (only when an HTTPS port is configured)
app.UseHttpsRedirection();

// Add error handling middleware
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new
    {
        message = "An unexpected error occurred while processing the request."
    });
}));

// Health endpoint: verifies process + database connectivity (used for
// deployment checks and uptime monitors; 503 when the DB is unreachable).
app.MapGet("/api/health", async (PortfolioDbContext db, CancellationToken cancellationToken) =>
{
    var now = DateTime.UtcNow;
    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "ok", database = "ok", time = now })
            : Results.Json(new { status = "degraded", database = "unreachable", time = now },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "degraded", database = "unreachable", time = now },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Root landing: never a dead 404 — send browsers to the Swagger UI (human
// landing page for the API) or, when Swagger is disabled, a JSON index.
app.MapGet("/", () => swaggerEnabled
    ? Results.Redirect("/swagger")
    : Results.Ok(new
    {
        name = "Portfolio API",
        version = "v1",
        health = "/api/health",
        publicApi = "/api/public/*"
    }));

// Map controllers
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PortfolioDbContext>();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> for the
// integration test project (top-level statements generate an internal class).
public partial class Program { }
