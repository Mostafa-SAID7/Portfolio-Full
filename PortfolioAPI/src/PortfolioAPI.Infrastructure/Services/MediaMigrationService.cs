using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>Outcome of one media-migration run.</summary>
    public sealed record MediaMigrationReport(
        string Provider,
        int Examined,
        int AlreadyOnProvider,
        int Migrated,
        int External,
        int Failed,
        bool DryRun,
        IReadOnlyList<string> Failures)
    {
        /// <summary>Human-readable block printed by the CLI command.</summary>
        public IReadOnlyList<string> ToLines()
        {
            var lines = new List<string>
            {
                $"Media migration report (provider: {Provider})",
                $"  Projects examined:      {Examined}",
                $"  Already on provider:    {AlreadyOnProvider}",
                $"  Migrated:               {Migrated}",
                $"  External / not managed: {External}",
                $"  Failed:                 {Failed}"
            };

            foreach (var failure in Failures)
            {
                lines.Add($"  FAILED: {failure}");
            }

            lines.Add(DryRun
                ? "  DRY RUN — nothing was changed."
                : "  Local files were kept as the rollback source.");

            return lines;
        }
    }

    /// <summary>Re-points existing media from local storage at the active provider.</summary>
    public interface IMediaMigrationService
    {
        /// <summary>Idempotent: rows already on the provider are skipped, so the command can be re-run.</summary>
        Task<MediaMigrationReport> RunAsync(bool dryRun, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Moves every project cover that still lives in <c>App_Data/uploads</c> to
    /// the active storage provider (Cloudinary in production).
    ///
    /// Safety rules:
    /// - <b>Idempotent</b>: a row already marked with the provider is skipped,
    ///   so an interrupted run can simply be repeated.
    /// - <b>Dry run</b>: <c>--dry-run</c> reports what would happen and touches
    ///   nothing.
    /// - <b>Rollback source kept</b>: local files are never deleted; if a
    ///   migration has to be undone, the original files are still on disk.
    /// - <b>Row-by-row commit</b>: each project is saved independently, so one
    ///   bad file cannot discard the work done for the others.
    /// - A Cloudinary upload that did not return a usable asset is treated as a
    ///   failure and the row is left untouched.
    /// </summary>
    public sealed class MediaMigrationService : IMediaMigrationService
    {
        private readonly PortfolioDbContext _db;
        private readonly IFileStorageService _storage;
        private readonly IImageValidator _validator;
        private readonly IConfiguration _configuration;
        private readonly string _localUploadPath;
        private readonly ILogger<MediaMigrationService> _logger;        public MediaMigrationService(
            PortfolioDbContext db,
            IFileStorageService storage,
            IImageValidator validator,
            IConfiguration configuration,
            ILogger<MediaMigrationService> logger)
        {
            _db = db;
            _storage = storage;
            _validator = validator;
            _configuration = configuration;
            _localUploadPath = UploadStorage.ResolveRoot(configuration);
            _logger = logger;
        }

        public async Task<MediaMigrationReport> RunAsync(bool dryRun, CancellationToken cancellationToken = default)
        {
            var examined = 0;
            var alreadyMigrated = 0;
            var migrated = 0;
            var external = 0;
            var failed = 0;
            var failures = new List<string>();

            // Active provider must be the target: migrating while "local" is
            // selected would be a no-op that looks like success.
            if (_storage.Provider != MediaProvider.Cloudinary)
            {
                return new MediaMigrationReport(
                    _storage.Provider, 0, 0, 0, 0, 1, dryRun,
                    new[] { $"Active storage provider is '{_storage.Provider}'. Set Storage:Provider=cloudinary before migrating." });
            }

            // Tracked on purpose: rows are mutated and saved back one by one
            // (a read-only query would make the SaveChanges below a no-op).
            var projects = await _db.Projects
                .OrderBy(p => p.Id)
                .ToListAsync(cancellationToken);

            foreach (var project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(project.ImageUrl))
                {
                    continue; // no cover — nothing to do
                }

                examined++;

                var currentProvider = MediaProvider.Normalize(project.StorageProvider);
                if (currentProvider == MediaProvider.Cloudinary)
                {
                    alreadyMigrated++;
                    continue;
                }

                // A Cloudinary URL without stored metadata predates the public
                // id column: backfilling it makes the row manageable without
                // re-uploading anything. The host check is scoped to THIS
                // cloud, so a foreign domain is treated as external media the
                // API does not own.
                if (CloudinaryStorageService.LooksLikeCloudinaryUrl(project.ImageUrl, ResolveCloudName()))
                {
                    if (dryRun)
                    {
                        alreadyMigrated++;
                        continue;
                    }

                    project.StorageProvider = MediaProvider.Cloudinary;
                    project.StoragePublicId = CloudinaryStorageService.TryGetPublicId(project.ImageUrl);
                    await _db.SaveChangesAsync(cancellationToken);
                    alreadyMigrated++;
                    continue;
                }

                if (!UploadStorage.IsManagedFile(project.ImageUrl, _configuration))
                {
                    external++; // GitHub avatar, bundled /images/... path, etc.
                    continue;
                }

                var localPath = Path.Combine(_localUploadPath, Path.GetFileName(project.ImageUrl));
                if (!File.Exists(localPath))
                {
                    failed++;
                    failures.Add($"Project {project.Id} ({project.Slug}): local file '{Path.GetFileName(project.ImageUrl)}' is missing.");
                    continue;
                }

                try
                {
                    var bytes = await File.ReadAllBytesAsync(localPath, cancellationToken);

                    // The same validator every upload goes through — a corrupt
                    // or unsupported legacy file is reported, not stored.
                    var validated = await _validator.ValidateAsync(
                        new MemoryStream(bytes),
                        Path.GetFileName(localPath),
                        contentType: null,
                        cancellationToken);

                    if (dryRun)
                    {
                        migrated++;
                        continue;
                    }

                    var stored = await _storage.UploadImageAsync(
                        new ImageUploadRequest
                        {
                            Content = new MemoryStream(validated.Content),
                            FileName = Path.GetFileName(localPath),
                            Slug = project.Slug,
                            AssetType = "cover"
                        },
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(stored.PublicId) || string.IsNullOrWhiteSpace(stored.Url))
                    {
                        failed++;
                        failures.Add($"Project {project.Id} ({project.Slug}): provider returned no public id or URL.");
                        continue;
                    }

                    // The DB row is only updated once the asset exists and is
                    // usable. The local file is deliberately kept: it is the
                    // rollback source until the CDN copy has been verified.
                    project.ImageUrl = stored.Url;
                    project.StorageProvider = MediaProvider.Cloudinary;
                    project.StoragePublicId = stored.PublicId;
                    await _db.SaveChangesAsync(cancellationToken);

                    migrated++;
                    _logger.LogInformation(
                        "Migrated cover of project {ProjectId} ({Slug}) to Cloudinary as {PublicId}",
                        project.Id, project.Slug, stored.PublicId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    failures.Add($"Project {project.Id} ({project.Slug}): {ex.Message}");
                    _logger.LogWarning(ex, "Failed to migrate cover of project {ProjectId} ({Slug})", project.Id, project.Slug);
                }
            }

            return new MediaMigrationReport(
                _storage.Provider, examined, alreadyMigrated, migrated, external, failed, dryRun, failures);
        }

        /// <summary>
        /// The cloud the migration must scope classifications to. Reads the
        /// same sources the startup resolver does (config section with the
        /// CLOUDINARY_* variable as a fallback).
        /// </summary>
        private string? ResolveCloudName()
        {
            var configured = _configuration["Cloudinary:CloudName"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            var fromEnvironment = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment;
        }
    }
}
