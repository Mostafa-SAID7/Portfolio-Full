using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PortfolioAPI.Controllers;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class LogsControllerTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<ILogger<LogsController>> _loggerMock;

        public LogsControllerTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PortfolioTests_Logs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            _environmentMock = new Mock<IWebHostEnvironment>();
            _environmentMock.Setup(e => e.ContentRootPath).Returns(_tempDirectory);

            _loggerMock = new Mock<ILogger<LogsController>>();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                try { Directory.Delete(_tempDirectory, true); } catch { }
            }
        }

        private LogsController CreateController(Dictionary<string, string?>? inMemoryConfig = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig ?? new Dictionary<string, string?>())
                .Build();

            return new LogsController(_environmentMock.Object, config, _loggerMock.Object);
        }

        [Fact]
        public void GetLogsUi_ReturnsHtmlContent_WithFullServerPathPrompt()
        {
            var controller = CreateController();

            var result = controller.GetLogsUi();

            var contentResult = result.Should().BeOfType<ContentResult>().Subject;
            contentResult.ContentType.Should().Contain("text/html");
            contentResult.Content.Should().Contain("Production Diagnostic Logs");
            contentResult.Content.Should().Contain("ACTIVE SERVER LOG PATH");
        }

        [Fact]
        public void GetLogInfo_ResolvesLogDirectoryAndFiles_Accurately()
        {
            var logsDir = Path.Combine(_tempDirectory, "App_Data", "logs");
            Directory.CreateDirectory(logsDir);

            var todayPrefix = $"portfolio-{DateTime.UtcNow:yyyyMMdd}";
            var testLogFile = Path.Combine(logsDir, $"{todayPrefix}.log");
            File.WriteAllText(testLogFile, "[2026-11-12 10:00:00.000 +00:00] [INF] [Test] Hello World\n");

            var controller = CreateController();

            var actionResult = controller.GetLogInfo();
            var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<LogInfoResponse>().Subject;

            response.DirectoryExists.Should().BeTrue();
            response.LogDirectory.Should().Be(logsDir);
            response.CurrentLogFile.Should().Be($"{todayPrefix}.log");
            response.CurrentLogFullPath.Should().Be(testLogFile);
            response.Files.Should().ContainSingle(f => f.Name == $"{todayPrefix}.log");
        }

        [Fact]
        public void GetLogEntries_FiltersByLevel_And_SearchTerm()
        {
            var logsDir = Path.Combine(_tempDirectory, "App_Data", "logs");
            Directory.CreateDirectory(logsDir);

            var testLogFile = Path.Combine(logsDir, "portfolio-test.log");
            File.WriteAllLines(testLogFile, new[]
            {
                "[2026-11-12 10:00:00.000 +00:00] [INF] [App.Startup] System started normally.",
                "[2026-11-12 10:01:00.000 +00:00] [WRN] [App.Security] Transient token refresh failure.",
                "[2026-11-12 10:02:00.000 +00:00] [ERR] [App.Database] Database connection failed on timeout.",
                "System.TimeoutException: Execution timed out at ...",
                "   at System.Threading.Tasks.Task.Wait()"
            });

            var controller = CreateController();

            // 1. Level filter for ERROR
            var errorResult = controller.GetLogEntries("portfolio-test.log", "ERROR", null, 100);
            var okError = errorResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var errorResponse = okError.Value.Should().BeOfType<LogEntriesResponse>().Subject;

            errorResponse.FilteredEntries.Should().Be(1);
            errorResponse.Entries[0].Level.Should().Be("Error");
            errorResponse.Entries[0].Exception.Should().Contain("System.TimeoutException");

            // 2. Search filter for "Startup"
            var searchResult = controller.GetLogEntries("portfolio-test.log", null, "Startup", 100);
            var okSearch = searchResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var searchResponse = okSearch.Value.Should().BeOfType<LogEntriesResponse>().Subject;

            searchResponse.FilteredEntries.Should().Be(1);
            searchResponse.Entries[0].Message.Should().Contain("System started normally.");
        }

        [Fact]
        public void GetLogEntries_DirectoryTraversal_ReturnsNotFound()
        {
            var logsDir = Path.Combine(_tempDirectory, "App_Data", "logs");
            Directory.CreateDirectory(logsDir);

            var controller = CreateController();

            var result = controller.GetLogEntries("../../appsettings.json", null, null, 100);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}

