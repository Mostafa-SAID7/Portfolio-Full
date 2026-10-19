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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

// Add repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IExperienceService, ExperienceService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITestimonialService, TestimonialService>();

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

// Restore real client IP/scheme behind the Vercel proxy / hosting load
// balancer — must run before anything that reads RemoteIpAddress (rate limiting).
app.UseForwardedHeaders();

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

// Error handling: known domain exceptions get a proper status + a REAL message
// (the dashboards render `message` verbatim); everything else stays a generic
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

// Health endpoint: verifies process + database connectivity (used for
// deployment checks and uptime monitors; 503 when the DB is unreachable).
// Served under the versioned path (canonical, issue #24) plus the legacy
// unversioned alias for existing monitors.
async Task<IResult> HealthAsync(PortfolioDbContext db, CancellationToken cancellationToken)
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
}

app.MapGet("/api/v1/health", HealthAsync);
app.MapGet("/api/health", HealthAsync); // deprecated alias (issue #24)

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
