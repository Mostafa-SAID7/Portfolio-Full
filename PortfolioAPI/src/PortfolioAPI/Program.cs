using PortfolioAPI.Application.Services;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using PortfolioAPI.Infrastructure.Services;
using PortfolioAPI.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortfolioAPI.Application.Exceptions;
using System.Text;
using System.Threading.RateLimiting;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

var builder = WebApplication.CreateBuilder(args);

// Swagger UI (issue #3/#8): OFF by default outside Development; every
// environment can still opt in explicitly with `Swagger__Enabled=true`
// (the production config does so deliberately). Docs also advertise the
// JWT bearer scheme while auth is enabled.
var swaggerEnabled = builder.Configuration.GetValue("Swagger:Enabled", builder.Environment.IsDevelopment());

// Add services to the container.
if (swaggerEnabled)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Portfolio API",
            Version = "v1",
            Description = "Centralized backend for the Portfolio admin dashboard and the public portfolio site. "
                          + "All JSON endpoints are versioned under /api/v1/*; the public, read-only surface used "
                          + "by the portfolio frontend lives under /api/v1/public/* (legacy unversioned paths stay "
                          + "available as deprecated aliases — see docs/API_VERSIONING.md)."
        });

        // Issue #24: document only the canonical /api/v1/... routes. The legacy
        // unversioned aliases keep working during the transition but stay out of
        // the spec so consumers only ever copy the versioned paths.
        options.DocInclusionPredicate((_, apiDescription) =>
            apiDescription.RelativePath?.Contains("v1/", StringComparison.Ordinal) == true);

        // Document the JWT security scheme (issues #1/#6): paste the token
        // from POST /api/v1/auth/login to try admin endpoints from this UI.
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Admin JWT from POST /api/v1/auth/login (paste the token value, Swagger adds the scheme prefix)."
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

// Add controllers
builder.Services.AddControllers();

// Increase startup time limit for IIS/ANCM (default 120s) - DbInitializer may need more time on cold start
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10485760; // 10MB
});

// Output caching for the public, read-only content endpoints (issue #32).
// Public portfolio content changes only when the dashboard saves, so a
// short TTL keeps SQL out of the hot path without any invalidation plumbing:
// a dashboard edit becomes visible at most one TTL later. Admin routes are
// never cached (the attribute is applied per action on PublicController).
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("public-content", policy =>
        policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("featured")
            .SetVaryByHeader("Origin")
            .Tag("public"));
});

// CORS: Development allows any origin (local Vite/Angular dev servers);
// every other environment only accepts the origins configured in
// `Cors:AllowedOrigins` (env: `Cors__AllowedOrigins`, comma-separated,
// e.g. "https://my-site.vercel.app,https://*.vercel.app" — a single "*"
// wildcard is allowed inside an origin pattern). With no origins configured,
// cross-origin requests are denied and only same-origin callers work.
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// Origin patterns are precompiled ONCE at startup: the previous version built
// and compiled a fresh regex per request on this hot path.
var exactOrigins = new HashSet<string>(
    allowedOrigins.Where(pattern => !pattern.Contains('*')),
    StringComparer.OrdinalIgnoreCase);
var originRegexes = allowedOrigins
    .Where(pattern => pattern.Contains('*'))
    .Select(pattern => new System.Text.RegularExpressions.Regex(
        "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
        | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        | System.Text.RegularExpressions.RegexOptions.Compiled))
    .ToArray();

// Response compression: the JSON payloads (projects/skills lists) are 8-9 KB
// uncompressed; gzip cuts them to ~1 KB, which matters a lot on the Vercel
// → MonsterASP hop and on mobile connections.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/problem+json",
        "application/octet-stream"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              // Expose the versioning signals (issue #24) so browser devtools
              // on the frontends can read them cross-origin.
              .WithExposedHeaders("X-API-Version", "Deprecation");

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin();
        }
        else if (allowedOrigins.Length > 0)
        {
            // Exact origins are compared directly; patterns containing '*'
            // (e.g. https://*.vercel.app) are matched against the compiled set.
            policy.SetIsOriginAllowed(origin =>
                exactOrigins.Contains(origin) || originRegexes.Any(r => r.IsMatch(origin)));
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Add logging
builder.Services.AddLogging();

// --- Admin authentication (issues #1 / #6 / #7 / #12) -----------------------
// JWT bearer guards every admin MUTATING endpoint ([Authorize]); /api/public/*
// and admin read endpoints stay anonymous. Sign in via POST /api/auth/login
// with Auth:Username / Auth:Password (empty = login fails closed with 503).
// The signing key resolves inside TokenService (Auth:SigningKey or an
// ephemeral per-process key — no secret ships in any config).
var tokenService = new TokenService(builder.Configuration);
builder.Services.AddSingleton<ITokenService>(tokenService);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth:Issuer"] ?? "PortfolioAPI",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"] ?? "PortfolioDashboard",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = tokenService.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        // Uniform { message } JSON for 401/403 so the dashboard interceptor
        // can show a real reason (pairs with issue #20).
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                // Standard challenge header (RFC 6750) so API tools/probes can
                // tell "missing/expired JWT" from a plain 401.
                context.Response.Headers["WWW-Authenticate"] = "Bearer";
                await context.Response.WriteAsJsonAsync(new { message = "Authentication required. Sign in to continue." });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "You do not have permission to perform this action." });
            }
        };
    });
builder.Services.AddAuthorization();

// --- Rate limiting (issue #10) ----------------------------------------------
// Per-client-IP fixed windows on the public contact form and on admin login.
// ForwardedHeaders (configured below) restores the real client IP behind the
// Vercel proxy / hosting load balancer so legitimate visitors don't share a bucket.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests — please wait a moment and try again." },
            cancellationToken);
    };

    options.AddFixedWindowLimiter("public-submit", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Real client IP + scheme behind proxies (X-Forwarded-For / X-Forwarded-Proto).
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

// Repositories: only the UnitOfWork is registered — it constructs its own
// repositories over the scoped DbContext. (The old open-generic
// `IRepository<>` registration was never resolved by anything.)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IExperienceService, ExperienceService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();

builder.Services.AddScoped<ICredentialService, CredentialService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Add infrastructure services
builder.Services.AddScoped<IEmailService, EmailService>();

// --- Storage (local disk or Cloudinary) ------------------------------------
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// Cloudinary settings come from the Cloudinary:* section, with the documented
// CLOUDINARY_* environment variables accepted as an alternative (explicit
// config values win). The merged instance is registered once so DI, the
// fail-fast check below and the gateway all see the same values.
var cloudinaryOptions = builder.Configuration
    .GetSection(CloudinaryOptions.SectionName)
    .Get<CloudinaryOptions>() ?? new CloudinaryOptions();

string PreferSetting(string configured, string environmentVariable) =>
    string.IsNullOrWhiteSpace(configured)
        ? Environment.GetEnvironmentVariable(environmentVariable) ?? string.Empty
        : configured;

cloudinaryOptions.CloudName = PreferSetting(cloudinaryOptions.CloudName, "CLOUDINARY_CLOUD_NAME");
cloudinaryOptions.ApiKey = PreferSetting(cloudinaryOptions.ApiKey, "CLOUDINARY_API_KEY");
cloudinaryOptions.ApiSecret = PreferSetting(cloudinaryOptions.ApiSecret, "CLOUDINARY_API_SECRET");

// Cloudinary's own console hands out a single CLOUDINARY_URL variable; accept
// it as well, but only for credentials not already set (it carries no folder).
if (!cloudinaryOptions.IsConfigured &&
    CloudinaryOptions.TryParseCloudinaryUrl(Environment.GetEnvironmentVariable("CLOUDINARY_URL"), out var fromUrl))
{
    if (string.IsNullOrWhiteSpace(cloudinaryOptions.CloudName))
        cloudinaryOptions.CloudName = fromUrl.CloudName;
    if (string.IsNullOrWhiteSpace(cloudinaryOptions.ApiKey))
        cloudinaryOptions.ApiKey = fromUrl.ApiKey;
    if (string.IsNullOrWhiteSpace(cloudinaryOptions.ApiSecret))
        cloudinaryOptions.ApiSecret = fromUrl.ApiSecret;
}

var cloudinaryFolder = PreferSetting(cloudinaryOptions.Folder, "CLOUDINARY_FOLDER");
cloudinaryOptions.Folder = string.IsNullOrWhiteSpace(cloudinaryFolder) ? "portfolio" : cloudinaryFolder;

builder.Services.AddSingleton(cloudinaryOptions);

builder.Services.AddSingleton<IImageValidator, ImageValidator>();
builder.Services.AddSingleton<ICloudinaryGateway, CloudinaryGateway>();
builder.Services.AddScoped<IMediaMigrationService, MediaMigrationService>();

// Provider selection: Storage:Provider explicitly, otherwise production runs
// on the CDN and development stays on local disk. appsettings.Production.json
// or env vars override either way.
var storageProvider = builder.Configuration["Storage:Provider"];
if (string.IsNullOrWhiteSpace(storageProvider))
{
    storageProvider = builder.Environment.IsProduction()
        ? MediaProvider.Cloudinary
        : MediaProvider.Local;
}
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<CloudinaryStorageService>();

if (string.Equals(storageProvider, MediaProvider.Cloudinary, StringComparison.OrdinalIgnoreCase))
{
    // Fail fast on incomplete Cloudinary configuration - the message must not
    // print values (requirement 21). Uses the same merged instance as DI.
    if (!cloudinaryOptions.IsConfigured)
        throw new InvalidOperationException("Cloudinary configuration is incomplete.");

    builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<CloudinaryStorageService>());
}
else
{
    builder.Services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<FileStorageService>());
}

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

// --- Media migration command -------------------------------------------------
// `dotnet run --project PortfolioAPI -- migrate-media [--dry-run]`
// Re-points every local cover at Cloudinary. Idempotent, keeps the local files
// as the rollback source, and prints a report instead of failing silently.
if (args.Contains("migrate-media", StringComparer.OrdinalIgnoreCase))
{
    var dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
    var migrationLogger = app.Logger;
    var exitCode = 0;

    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<PortfolioDbContext>();

            // The storage columns must exist before rows are updated.
            await DbInitializer.InitializeAsync(db);

            var report = await services
                .GetRequiredService<IMediaMigrationService>()
                .RunAsync(dryRun);

            foreach (var line in report.ToLines())
            {
                Console.WriteLine(line);
            }

            if (report.Failed > 0)
            {
                exitCode = 1;
            }
        }
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "Media migration failed.");
        Console.Error.WriteLine($"Media migration failed: {ex.Message}");
        exitCode = 1;
    }

    Environment.ExitCode = exitCode;
    return;
}

// Restore real client IP/scheme behind the hosting reverse proxy — first,
// since everything downstream (rate limiting, redirects) reads them.
//
// Deliberately clearing KnownProxies/KnownNetworks: behind a single
// appending proxy the middleware then identifies the client from the NEAREST
// forwarded entry, which the proxy itself appended — clients cannot mint rate
// limit buckets by forging X-Forwarded-For. This is only safe while clients
// cannot reach Kestrel directly; if you ever expose the port publicly,
// populate KnownProxies/KnownNetworks instead of clearing them.
app.UseForwardedHeaders();

// Error handling at the TOP so faults anywhere below — CORS, static uploads,
// Swagger, controllers — get the shared JSON shape instead of a blank 500.
// Known domain exceptions get a proper status + a REAL message (the
// dashboards render `message` verbatim); everything else stays a generic
// 500 so production never leaks stack traces. Before this mapping, a missing
// entity or a FluentValidation failure from any service surfaced as a 500
// "An unexpected error occurred..." — validation messages were silently lost.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    int status;
    string message;
    IDictionary<string, string[]>? errors = null;

    switch (exception)
    {
        case NotFoundException notFound:
            status = StatusCodes.Status404NotFound;
            message = notFound.Message;
            break;
        case ValidationException validation:
            status = StatusCodes.Status400BadRequest;
            message = validation.Message;
            errors = validation.Errors;
            break;
        // Unique indexes (Skills.Name) reject duplicates at the
        // database level — surface them as a clear 409 instead of a generic 500.
        case DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 } }:
            status = StatusCodes.Status409Conflict;
            message = "That name is already taken — choose a different one.";
            break;
        default:
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PortfolioAPI.ExceptionHandler");
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            status = StatusCodes.Status500InternalServerError;
            message = "An unexpected error occurred while processing the request.";
            break;
    }

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/json";
    if (errors is null)
    {
        await context.Response.WriteAsJsonAsync(new { message });
    }
    else
    {
        await context.Response.WriteAsJsonAsync(new { message, errors });
    }
}));

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
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Use CORS (policy name set up above) — MUST sit before UseResponseCompression
// and UseOutputCache so cross-origin headers are attached to every response,
// including cached public content.
app.UseCors("Frontends");

// Compress JSON responses (gzip) — must sit before the endpoints write.
app.UseResponseCompression();

// Output cache (public content only - see AddOutputCache above). Must run
// before the endpoints so cached responses skip the action entirely.
app.UseOutputCache();

// Cache-Control for the public content endpoints (issue #32). The output cache
// middleware keeps SQL out of the hot path; these headers let browsers and the
// Vercel edge reuse the response for the same TTL. Only anonymous GETs under
// the public prefix are affected - admin routes, auth and mutations never
// match, so they stay uncached.
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

// Error handling and transport security are re-inserted at the TOP of the
// pipeline below (review finding: they must wrap CORS/static files/Swagger).

// Health endpoint: verifies process + database connectivity (used for
// deployment checks and uptime monitors; 503 when the DB is unreachable).
// Served under the versioned path (canonical, issue #24) plus the legacy
// unversioned alias for existing monitors.
async Task<IResult> HealthAsync(PortfolioDbContext db, IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
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

app.MapGet("/api/v1/health", HealthAsync);
app.MapGet("/api/health", HealthAsync); // deprecated alias (issue #24)

// Version root: makes the Deprecation `Link` successor target resolve (it
// used to point at /api/v1, which was a dead 404) and gives human consumers
// a discoverable entry point.
app.MapGet("/api/v1", () => Results.Ok(new
{
    name = "Portfolio API",
    version = "v1",
    health = "/api/v1/health",
    publicApi = "/api/v1/public/*",
    docs = "/swagger"
}));

// Root landing: never a dead 404 — send browsers to the Swagger UI (human
// landing page for the API) or, when Swagger is disabled, a JSON index.
app.MapGet("/", () => swaggerEnabled
    ? Results.Redirect("/swagger")
    : Results.Ok(new
    {
        name = "Portfolio API",
        version = "v1",
        health = "/api/v1/health",
        publicApi = "/api/v1/public/*"
    }));

// --- API versioning (issue #24) ---------------------------------------------
// JSON endpoints answer under /api/v1/... (canonical) and on the legacy
// unversioned paths as deprecated aliases: responses advertise the version,
// and the aliases carry Deprecation/Link headers. Static file serving
// (/api/files/...) is intentionally exempt — those URLs are immutable
// content stored in the database, not a versioned contract.
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
        await DbInitializer.InitializeAsync(context);
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
