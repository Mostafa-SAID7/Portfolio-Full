using Microsoft.AspNetCore.Hosting;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Storage wiring — extracted from Program.cs. One file owns everything
    /// media-related at startup: option binding, the documented CLOUDINARY_*
    /// / CLOUDINARY_URL environment overlay, provider selection and fail-fast
    /// on incomplete Cloudinary configuration. No secret value is ever logged
    /// from here (requirement 21).
    /// </summary>
    public static class StorageConfiguration
    {
        public static IServiceCollection AddStorage(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
            services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

            // Cloudinary settings come from the Cloudinary:* section, with the documented
            // CLOUDINARY_* environment variables accepted as an alternative (explicit
            // config values win). The merged instance is registered once so DI, the
            // fail-fast check below and the gateway all see the same values.
            var cloudinaryOptions = configuration
                .GetSection(CloudinaryOptions.SectionName)
                .Get<CloudinaryOptions>() ?? new CloudinaryOptions();

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

            services.AddSingleton(cloudinaryOptions);

            services.AddSingleton<IImageValidator, ImageValidator>();
            services.AddSingleton<ICloudinaryGateway, CloudinaryGateway>();
            services.AddScoped<IMediaMigrationService, MediaMigrationService>();

            // Provider selection: Storage:Provider explicitly, otherwise production runs
            // on the CDN and development stays on local disk. appsettings.Production.json
            // or env vars override either way.
            var storageProvider = configuration["Storage:Provider"];
            if (string.IsNullOrWhiteSpace(storageProvider))
            {
                storageProvider = environment.IsProduction()
                    ? MediaProvider.Cloudinary
                    : MediaProvider.Local;
            }
            services.AddScoped<FileStorageService>();
            services.AddScoped<CloudinaryStorageService>();

            if (string.Equals(storageProvider, MediaProvider.Cloudinary, StringComparison.OrdinalIgnoreCase))
            {
                // Fail fast on incomplete Cloudinary configuration - the message must not
                // print values (requirement 21). Uses the same merged instance as DI.
                if (!cloudinaryOptions.IsConfigured)
                    throw new InvalidOperationException("Cloudinary configuration is incomplete.");

                services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<CloudinaryStorageService>());
            }
            else
            {
                services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<FileStorageService>());
            }

            return services;
        }

        /// <summary>
        /// Backend-owned upload storage (project covers etc.): keep it OUTSIDE
        /// wwwroot (App_Data) so redeploying the publish output can never clobber
        /// uploaded files, and anchor it to the content root so it works the same
        /// under `dotnet run` and IIS/ANCM hosting. Mutates the configuration so
        /// the bound FileStorageOptions and the static-file middleware agree.
        /// </summary>
        public static void AnchorUploadPath(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configuredStoragePath = configuration["FileStorage:Path"];
            configuration["FileStorage:Path"] =
                string.IsNullOrWhiteSpace(configuredStoragePath)
                    ? Path.Combine(environment.ContentRootPath, UploadStorage.DefaultRelativePath)
                    : Path.IsPathRooted(configuredStoragePath)
                        ? configuredStoragePath
                        : Path.Combine(environment.ContentRootPath, configuredStoragePath);
        }

        private static string PreferSetting(string configured, string environmentVariable) =>
            string.IsNullOrWhiteSpace(configured)
                ? Environment.GetEnvironmentVariable(environmentVariable) ?? string.Empty
                : configured;
    }
}
