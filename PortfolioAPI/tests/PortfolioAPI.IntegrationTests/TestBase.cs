using System.Net.Http.Headers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using PortfolioAPI.Infrastructure.Services;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class TestBase : IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer;
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;
        protected readonly IServiceScope _scope;
        protected readonly PortfolioDbContext _dbContext;

        public TestBase()
        {
            // Configure SQL Server test container (Testcontainers v3 API)
            _dbContainer = new MsSqlBuilder()
                .WithPassword("Strong_password_123!")
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithCleanUp(true)
                .Build();

            // Configure web application factory with test container
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        // Remove the existing DbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<PortfolioDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add test database context
                        services.AddDbContext<PortfolioDbContext>(options =>
                        {
                            options.UseSqlServer(_dbContainer.GetConnectionString());
                        });

                        // Replace services with test implementations if needed
                        services.Replace(ServiceDescriptor.Scoped<IEmailService, TestEmailService>());
                        services.Replace(ServiceDescriptor.Scoped<IFileStorageService, TestFileStorageService>());
                    });

                    builder.UseConfiguration(new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                            ["EmailSettings:SmtpServer"] = "localhost",
                            ["EmailSettings:SmtpPort"] = "1025",
                            ["EmailSettings:Username"] = "test@example.com",
                            ["EmailSettings:Password"] = "password",
                            ["EmailSettings:FromEmail"] = "test@example.com",
                            ["EmailSettings:FromName"] = "Test Portfolio API",
                            ["FileStorage:BasePath"] = Path.Combine(Path.GetTempPath(), "portfolio-tests", Guid.NewGuid().ToString()),
                            ["FileStorage:MaxFileSize"] = "10485760",
                            ["FileStorage:AllowedExtensions:0"] = ".jpg",
                            ["FileStorage:AllowedExtensions:1"] = ".png",
                            ["FileStorage:AllowedExtensions:2"] = ".pdf"
                        })
                        .Build());
                });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            // Ensure database is created and migrations are applied
            await _dbContext.Database.MigrateAsync();

            // Seed test data if needed
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
            _scope.Dispose();
            _factory.Dispose();
        }

        protected virtual async Task SeedTestDataAsync()
        {
            // Override in derived classes to seed specific test data
        }

        protected async Task<T> GetServiceAsync<T>() where T : notnull
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        protected async Task<HttpResponseMessage> GetAsync(string uri, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.SendAsync(request);
        }

        protected async Task<HttpResponseMessage> PostAsync<T>(string uri, T content, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(content),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.SendAsync(request);
        }

        protected async Task<HttpResponseMessage> PutAsync<T>(string uri, T content, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(content),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.SendAsync(request);
        }

        protected async Task<HttpResponseMessage> DeleteAsync(string uri, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, uri);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.SendAsync(request);
        }

        protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    // Test implementations of external services
    public class TestEmailService : IEmailService
    {
        public List<EmailMessage> SentEmails { get; } = new();

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            SentEmails.Add(new EmailMessage
            {
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = isHtml,
                SentAt = DateTime.UtcNow
            });
            return Task.CompletedTask;
        }

        public Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message)
        {
            return SendEmailAsync("admin@test.com", $"Contact: {subject}", $"From: {fromName} ({fromEmail})\n\n{message}");
        }

        public Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            return SendEmailAsync(toEmail, "Welcome!", $"Welcome {userName}!");
        }

        public Task SendNotificationEmailAsync(string toEmail, string title, string content)
        {
            return SendEmailAsync(toEmail, title, content);
        }
    }

    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class TestFileStorageService : IFileStorageService
    {
        private readonly Dictionary<string, (Stream Stream, string ContentType)> _storedFiles = new();
        private readonly string _testDirectory;

        public TestFileStorageService()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "portfolio-test-files", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var fileId = Guid.NewGuid().ToString();
            var filePath = Path.Combine(_testDirectory, fileId);

            using (var fileStreamToWrite = File.Create(filePath))
            {
                fileStream.CopyTo(fileStreamToWrite);
            }

            _storedFiles[fileId] = (File.OpenRead(filePath), contentType);
            return Task.FromResult($"/test-files/{fileId}");
        }

        public Task<Stream> GetFileAsync(string fileUrl)
        {
            var fileId = Path.GetFileName(fileUrl);
            if (_storedFiles.TryGetValue(fileId, out var fileData))
            {
                return Task.FromResult(fileData.Stream);
            }
            throw new FileNotFoundException();
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            var fileId = Path.GetFileName(fileUrl);
            _storedFiles.Remove(fileId);
            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string fileUrl)
        {
            var fileId = Path.GetFileName(fileUrl);
            return Task.FromResult(_storedFiles.ContainsKey(fileId));
        }

        public Task<long> GetFileSizeAsync(string fileUrl)
        {
            var fileId = Path.GetFileName(fileUrl);
            if (_storedFiles.TryGetValue(fileId, out var fileData))
            {
                return Task.FromResult(fileData.Stream.Length);
            }
            throw new FileNotFoundException();
        }

        public Task<string> SaveImageAsync(Stream imageStream, string fileName)
        {
            return SaveFileAsync(imageStream, fileName, "image/jpeg");
        }

        public Task<string> SaveDocumentAsync(Stream documentStream, string fileName)
        {
            return SaveFileAsync(documentStream, fileName, "application/pdf");
        }
    }
}