using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using PortfolioAPI.Configuration;
using Serilog;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class LoggingConfigurationTests
    {
        [Fact]
        public void CreateBootstrapLogger_ReturnsValidLogger()
        {
            // Act
            var logger = LoggingConfiguration.CreateBootstrapLogger();

            // Assert
            logger.Should().NotBeNull();
        }

        [Fact]
        public void ConfigureSerilog_InDevelopment_ConfiguresSuccessfully()
        {
            // Arrange
            var loggerConfig = new LoggerConfiguration();
            var config = new ConfigurationBuilder().Build();
            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            // Act & Assert
            var act = () => LoggingConfiguration.ConfigureSerilog(loggerConfig, config, envMock.Object);
            act.Should().NotThrow();
        }

        [Fact]
        public void ConfigureSerilog_InProductionWithFileLogging_ConfiguresSuccessfully()
        {
            // Arrange
            var loggerConfig = new LoggerConfiguration();
            var tempDir = Path.Combine(Path.GetTempPath(), "PortfolioLogsTest_" + System.Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["Logging:File:Enabled"] = "true",
                ["Logging:File:Path"] = tempDir,
                ["Logging:Console:Json"] = "true"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            envMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());

            try
            {
                // Act & Assert
                var act = () => LoggingConfiguration.ConfigureSerilog(loggerConfig, config, envMock.Object);
                act.Should().NotThrow();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}

