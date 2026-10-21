using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Database wiring — extracted from Program.cs: fail-fast connection-string
    /// resolution, the SQL Server context and the single UnitOfWork.
    /// </summary>
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Connection string: provided by the environment in production
            // (`ConnectionStrings__DefaultConnection`) and by the untracked
            // appsettings.Development.json (or user-secrets) locally. Fail fast
            // with a clear message instead of a provider error on the first request.
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured. Set the ConnectionStrings__DefaultConnection " +
                    "environment variable (production) or appsettings.Development.json / user-secrets (local development).");
            }

            // Add database context (SQL Server from ConnectionStrings:DefaultConnection)
            services.AddDbContext<PortfolioDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.EnableRetryOnFailure()));

            // Repositories: only the UnitOfWork is registered — it constructs its own
            // repositories over the scoped DbContext. (The old open-generic
            // `IRepository<>` registration was never resolved by anything.)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
