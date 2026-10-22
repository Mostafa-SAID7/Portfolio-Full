using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PortfolioAPI.Auth;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Admin authentication — extracted from Program.cs.
    /// </summary>
    public static class JwtAuthenticationConfiguration
    {
        public static IServiceCollection AddAdminAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // --- Admin authentication (issues #1 / #6 / #7 / #12) -----------------
            // JWT bearer guards every admin MUTATING endpoint ([Authorize]);
            // /api/public/* and admin read endpoints stay anonymous. Sign in via
            // POST /api/auth/login with Auth:Username / Auth:Password (empty =
            // login fails closed with 503). The signing key resolves inside
            // TokenService (Auth:SigningKey or an ephemeral per-process key —
            // no secret ships in any config).
            var tokenService = new TokenService(configuration);
            services.AddSingleton<ITokenService>(tokenService);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Auth:Issuer"] ?? "PortfolioAPI",
                        ValidateAudience = true,
                        ValidAudience = configuration["Auth:Audience"] ?? "PortfolioDashboard",
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

            services.AddAuthorization();

            return services;
        }
    }
}
