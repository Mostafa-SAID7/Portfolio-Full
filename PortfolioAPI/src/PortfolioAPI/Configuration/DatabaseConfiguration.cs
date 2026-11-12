using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using Serilog;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Database wiring — extracted from Program.cs: connection-string resolution,
    /// resilient SQL Server context registration, and the single UnitOfWork.
    /// </summary>
    public static class DatabaseConfiguration
    {
        public const string FallbackConnectionString =
            "Server=tcp:127.0.0.1,1433;Database=Unconfigured_PortfolioDb;User Id=unconfigured;Password=unconfigured;Connect Timeout=2;TrustServerCertificate=True;";

        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Never crash Web API host startup with IIS 500.30 when connection string is missing or delayed.
                // Log a clear, actionable warning so the operator sees exactly what setting is required in MonsterASP.
                Log.Warning(
                    "DATABASE CONFIGURATION WARNING: Connection string 'DefaultConnection' is not configured. " +
                    "Set the 'ConnectionStrings__DefaultConnection' environment variable (production) or " +
                    "appsettings.Development.json / user-secrets (local). The API host will start in degraded mode " +
                    "and report health status at /api/v1/health.");

                connectionString = FallbackConnectionString;
            }

            // Add database context (SQL Server)
            services.AddDbContext<PortfolioDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

            // Repositories: UnitOfWork constructs its own repositories over the scoped DbContext
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
