using Microsoft.OpenApi.Models;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Swagger documentation wiring — extracted from Program.cs so one concern
    /// lives in one file. The API surface doc in here mirrors docs/API_VERSIONING.md.
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// Swagger UI: enabled by default across all environments (including production)
        /// so the interactive API documentation and test console are immediately available.
        /// Can be opted out explicitly with `Swagger:Enabled=false` or `Swagger__Enabled=false`.
        /// </summary>
        public static bool IsEnabled(IConfiguration configuration, IWebHostEnvironment environment) =>
            configuration.GetValue("Swagger:Enabled", true);

        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Portfolio API",
                    Version = "v1",
                    Description = "Centralized backend for the Portfolio admin dashboard and the public portfolio site. "
                                  + "All JSON endpoints are versioned under /api/v1/*; the public, read-only surface used "
                                  + "by the portfolio frontend lives under /api/v1/public/* (the legacy unversioned "
                                  + "aliases have been removed — see docs/API_VERSIONING.md)."
                });

                // Issue #128: only /api/v1/... routes exist. The former unversioned
                // aliases were removed, so the spec only ever shows versioned paths.
                options.DocInclusionPredicate((_, apiDescription) =>
                    apiDescription.RelativePath?.Contains("v1/", StringComparison.Ordinal) == true);

                // Document the JWT security scheme (issues #1/#6): paste the token
                // from POST /api/v1/auth/login to try admin endpoints from this UI.
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Admin JWT from POST /api/v1/auth/login (paste the token value, Swagger adds the scheme prefix)."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
