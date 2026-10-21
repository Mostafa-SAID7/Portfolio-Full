using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Admin-only media operations.
    ///
    /// The hosting panel (runasp.net) has no shell access, so the
    /// <c>migrate-media</c> CLI command cannot be run there. This endpoint
    /// exposes the <b>same</b> <see cref="IMediaMigrationService"/> over an
    /// authenticated request instead: same idempotency, same dry-run, same
    /// report — just triggered from the dashboard, Swagger or curl.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/admin/media")]
    [Route("api/admin/media")]
    public class AdminMediaController : ControllerBase
    {
        private readonly IMediaMigrationService _migration;

        public AdminMediaController(IMediaMigrationService migration)
        {
            _migration = migration;
        }

        /// <summary>
        /// Re-points every locally-stored cover at the active provider.
        /// Idempotent, keeps the local files as the rollback source.
        /// </summary>
        /// <param name="dryRun">true = classify only, change nothing; run this first.</param>
        [HttpPost("migrate")]
        public async Task<IActionResult> Migrate([FromQuery] bool dryRun, CancellationToken cancellationToken)
        {
            MediaMigrationReport report;
            try
            {
                report = await _migration.RunAsync(dryRun, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception)
            {
                // No provider detail: the real cause is in the server log.
                return StatusCode(500, new { message = "The media migration failed. Check the server logs." });
            }

            return Ok(new
            {
                report.Provider,
                report.Examined,
                report.AlreadyOnProvider,
                report.Migrated,
                report.External,
                report.Failed,
                report.DryRun,
                Failures = report.Failures
            });
        }
    }
}
