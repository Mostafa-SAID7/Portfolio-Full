using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PortfolioAPI.Configuration
{
    /// <summary>
    /// Centralized Serilog structured logging configuration for Portfolio API.
    /// Manages bootstrap logging, host integration, environment-specific sinks,
    /// category-based level filtering, and LogContext enrichment.
    /// </summary>
    public static class LoggingConfiguration
    {
        private const string OutputTemplate =
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        /// <summary>
        /// Creates a bootstrap logger for capturing startup and configuration failures
        /// before the host service provider is fully constructed.
        /// </summary>
        public static Serilog.ILogger CreateBootstrapLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .CreateBootstrapLogger();
        }

        /// <summary>
        /// Registers Serilog as the primary logging provider on the WebApplicationBuilder host.
        /// </summary>
        public static WebApplicationBuilder AddEnterpriseLogging(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Host.UseSerilog((context, services, loggerConfig) =>
            {
                ConfigureSerilog(loggerConfig, context.Configuration, context.HostingEnvironment);
            });

            return builder;
        }

        /// <summary>
        /// Configures Serilog sinks, minimum log levels, filters, and enrichers
        /// using application configuration and environment context.
        /// </summary>
        public static void ConfigureSerilog(
            LoggerConfiguration loggerConfig,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(loggerConfig);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            // Establish safe default log levels per category to prevent log flooding
            var defaultLevel = environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;
            loggerConfig
                .MinimumLevel.Is(defaultLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);

            // Read base overrides and settings from IConfiguration (Serilog: section if present)
            loggerConfig
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PortfolioAPI")
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

            // Console sink configuration
            var useJsonConsole = configuration.GetValue("Logging:Console:Json", false) ||
                                 string.Equals(configuration["Logging:Console:Formatter"], "json", StringComparison.OrdinalIgnoreCase);

            if (useJsonConsole)
            {
                loggerConfig.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                loggerConfig.WriteTo.Console(outputTemplate: OutputTemplate);
            }

            // Rolling file sink configuration (defaults to true so App_Data/logs is created and populated)
            var fileLoggingEnabled = configuration.GetValue("Logging:File:Enabled", true);
            if (fileLoggingEnabled)
            {
                try
                {
                    var configuredPath = configuration["Logging:File:Path"];
                    var baseDirectory = string.IsNullOrWhiteSpace(environment.ContentRootPath)
                        ? AppContext.BaseDirectory
                        : environment.ContentRootPath;

                    var logDirectory = string.IsNullOrWhiteSpace(configuredPath)
                        ? Path.Combine(baseDirectory, "App_Data", "logs")
                        : (Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(baseDirectory, configuredPath));

                    Directory.CreateDirectory(logDirectory);
                    var logFilePath = Path.Combine(logDirectory, "portfolio-.log");

                    loggerConfig.WriteTo.File(
                        path: logFilePath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB per file
                        rollOnFileSizeLimit: true,
                        outputTemplate: OutputTemplate);
                }
                catch (Exception ex)
                {
                    // File sink initialization failure (e.g. restricted IIS AppPool directory permissions)
                    // must NEVER crash the entire Web API host during startup (prevents 500.30).
                    Console.Error.WriteLine($"[Serilog Warning] Failed to initialize file logger sink: {ex.Message}");
                }
            }
        }
    }
}
