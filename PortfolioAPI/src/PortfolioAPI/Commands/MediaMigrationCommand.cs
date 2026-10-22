using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Commands
{
    /// <summary>
    /// CLI command — extracted from Program.cs:
    /// `dotnet run --project PortfolioAPI -- migrate-media [--dry-run]`
    /// Re-points every local cover at Cloudinary. Idempotent, keeps the local
    /// files as the rollback source, and prints a report instead of failing
    /// silently. (On shell-less hosting panel there is an authenticated admin
    /// endpoint that runs the same service — see AdminMediaController.)
    /// </summary>
    public static class MediaMigrationCommand
    {
        public static bool IsRequested(string[] args) =>
            args.Contains("migrate-media", StringComparer.OrdinalIgnoreCase);

        public static async Task<int> ExecuteAsync(WebApplication app, string[] args)
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
                    await DbInitializer.InitializeAsync(db, services.GetRequiredService<ILogger<PortfolioDbContext>>());

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

            return exitCode;
        }
    }
}
