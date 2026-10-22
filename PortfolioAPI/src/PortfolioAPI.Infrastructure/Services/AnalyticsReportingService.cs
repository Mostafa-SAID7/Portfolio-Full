using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Exceptions;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// GA4 configuration (appsettings "Analytics" section or environment
    /// variables). The service account key is a server-side secret: it must
    /// never be committed and never reaches the browser.
    /// </summary>
    public class AnalyticsOptions
    {
        /// <summary>GA4 property id, e.g. "345678901".</summary>
        public string PropertyId { get; set; } = string.Empty;
        /// <summary>Service account e-mail (…@…iam.gserviceaccount.com).</summary>
        public string ServiceAccountEmail { get; set; } = string.Empty;
        /// <summary>PEM private key of the service account.</summary>
        public string PrivateKey { get; set; } = string.Empty;
        /// <summary>How long a successful report is reused (the freshness window).</summary>
        public int CacheSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Reads GA4 reports through the official Data API (v1beta).
    ///
    /// Authentication is the standard Google service-account flow: a short
    /// lived RS256 assertion is exchanged for an access token, which is cached
    /// until it expires. The report itself is cached for a few minutes so the
    /// dashboard never burns the GA4 quota on every refresh.
    /// </summary>
    public class AnalyticsReportingService : IAnalyticsReportingService
    {
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string Scope = "https://www.googleapis.com/auth/analytics.readonly";
        private const string ReportEndpoint = "https://analyticsdata.googleapis.com/v1beta/properties/{0}:runReport";
        private const string CacheKey = "ga4-report";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<AnalyticsOptions> _options;
        private readonly ILogger<AnalyticsReportingService> _logger;
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private readonly Dictionary<string, (DateTime ExpiresAt, AnalyticsReportDto Report)> _cache = new();

        private string? _accessToken;
        private DateTime _accessTokenExpiresAt = DateTime.MinValue;

        public AnalyticsReportingService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<AnalyticsOptions> options,
            ILogger<AnalyticsReportingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                var o = _options.CurrentValue;
                return !string.IsNullOrWhiteSpace(o.PropertyId)
                    && !string.IsNullOrWhiteSpace(o.ServiceAccountEmail)
                    && !string.IsNullOrWhiteSpace(o.PrivateKey);
            }
        }

        public async Task<AnalyticsReportDto> GetReportAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;
            if (!IsConfigured)
            {
                throw new AnalyticsNotConfiguredException(
                    "GA4 is not configured. Set Analytics:PropertyId, Analytics:ServiceAccountEmail and Analytics:PrivateKey.");
            }

            var key = $"{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";

            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(key, out var cached)
                    && cached.ExpiresAt > DateTime.UtcNow)
                {
                    return cached.Report;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            var report = await BuildReportAsync(options, startDate, endDate, cancellationToken);

            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                var ttl = TimeSpan.FromSeconds(Math.Clamp(options.CacheSeconds, 30, 3600));
                _cache[CacheKey + "|" + key] = (DateTime.UtcNow.Add(ttl), report);
            }
            finally
            {
                _cacheLock.Release();
            }

            return report;
        }

        private async Task<AnalyticsReportDto> BuildReportAsync(
            AnalyticsOptions options,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken)
        {
            var range = new AnalyticsRangeDto
            {
                StartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDate = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Days = endDate.DayNumber - startDate.DayNumber + 1
            };
            var report = new AnalyticsReportDto
            {
                GeneratedAt = DateTime.UtcNow
            };

            var accessToken = await GetAccessTokenAsync(options, cancellationToken);
            var client = _httpClientFactory.CreateClient("ga4");

            // The previous, equally long period – the dashboard compares against it.
            var days = range.Days;
            var previousEnd = startDate.AddDays(-1);
            var previousStart = previousEnd.AddDays(-(days - 1));

            var overviewTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: Array.Empty<string>(),
                metrics: new[] { "activeUsers", "newUsers", "totalUsers", "sessions", "screenPageViews", "engagedSessions", "averageSessionDuration" },
                cancellationToken);
            var previousTask = RunReportAsync(client, accessToken, options.PropertyId, previousStart, previousEnd,
                dimensions: Array.Empty<string>(),
                metrics: new[] { "activeUsers", "newUsers", "totalUsers", "sessions", "screenPageViews", "engagedSessions", "averageSessionDuration" },
                cancellationToken);
            var trendTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "date" },
                metrics: new[] { "activeUsers", "sessions", "screenPageViews" },
                cancellationToken);
            var countryTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "country" },
                metrics: new[] { "activeUsers" },
                cancellationToken);
            var sourceTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "sessionSource", "sessionMedium" },
                metrics: new[] { "sessions", "activeUsers" },
                cancellationToken);
            var pageTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "pagePath", "pageTitle" },
                metrics: new[] { "screenPageViews", "activeUsers" },
                cancellationToken: cancellationToken,
                limit: 25);
            var deviceTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "deviceCategory" },
                metrics: new[] { "activeUsers" },
                cancellationToken);
            var eventTask = RunReportAsync(client, accessToken, options.PropertyId, startDate, endDate,
                dimensions: new[] { "eventName" },
                metrics: new[] { "eventCount", "totalUsers" },
                cancellationToken: cancellationToken,
                limit: 25);

            await Task.WhenAll(overviewTask, previousTask, trendTask, countryTask, sourceTask, pageTask, deviceTask, eventTask);

            var overviewRows = await overviewTask;
            var previousRows = await previousTask;
            var trendRows = await trendTask;
            var countryRows = await countryTask;
            var sourceRows = await sourceTask;
            var pageRows = await pageTask;
            var deviceRows = await deviceTask;
            var eventRows = await eventTask;

            var overview = MapOverview(overviewRows, range);
            var previous = MapOverview(previousRows, new AnalyticsRangeDto
            {
                StartDate = previousStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                EndDate = previousEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Days = days
            });

            // The contact conversion is an event, not a metric.
            var conversions = eventRows
                .Where(r => string.Equals(r.Dimensions.ElementAtOrDefault(0), "contact_form_submission", StringComparison.Ordinal))
                .Sum(r => Metric(r, 0));
            overview.ContactConversions = conversions;
            previous.ContactConversions = previousRows
                .Where(r => string.Equals(r.Dimensions.ElementAtOrDefault(0), "contact_form_submission", StringComparison.Ordinal))
                .Sum(r => Metric(r, 0));

            report.Overview = overview;
            report.Overview.Previous = previous;
            report.Trend = trendRows.Select(r => new AnalyticsTrendPointDto
            {
                Date = r.Dimensions.ElementAtOrDefault(0),
                Users = Metric(r, 0),
                Sessions = Metric(r, 1),
                PageViews = Metric(r, 2)
            }).ToList();
            report.Countries = MapShares(countryRows, 0);
            report.Sources = sourceRows.Select(r => new AnalyticsSourceRowDto
            {
                Source = r.Dimensions.ElementAtOrDefault(0),
                Medium = r.Dimensions.ElementAtOrDefault(1),
                Sessions = Metric(r, 0),
                Users = Metric(r, 1)
            }).ToList();
            report.Pages = pageRows.Select(r => new AnalyticsPageRowDto
            {
                Path = r.Dimensions.ElementAtOrDefault(0),
                Title = r.Dimensions.ElementAtOrDefault(1),
                PageViews = Metric(r, 0),
                Users = Metric(r, 1)
            }).ToList();
            report.Devices = MapDevices(deviceRows);
            report.Events = eventRows.Select(r => new AnalyticsEventRowDto
            {
                Name = r.Dimensions.ElementAtOrDefault(0),
                Count = Metric(r, 0),
                Users = Metric(r, 1)
            }).ToList();

            return report;
        }

        private static AnalyticsOverviewDto MapOverview(IReadOnlyList<Row> rows, AnalyticsRangeDto range)
        {
            var overview = new AnalyticsOverviewDto { Range = range };
            if (rows.Count == 0) return overview;
            var metrics = rows[0].Metrics;
            overview.ActiveUsers = MetricAt(metrics, 0);
            overview.NewUsers = MetricAt(metrics, 1);
            overview.TotalUsers = MetricAt(metrics, 2);
            overview.Sessions = MetricAt(metrics, 3);
            overview.PageViews = MetricAt(metrics, 4);
            overview.EngagedSessions = MetricAt(metrics, 5);
            var average = metrics.Count > 6 ? ParseDouble(metrics[6]) : 0d;
            overview.AverageEngagementSeconds = average;
            overview.EngagementRate = overview.Sessions > 0
                ? Math.Round(overview.EngagedSessions * 100d / overview.Sessions, 1)
                : 0d;
            return overview;
        }

        private static List<AnalyticsGeoRowDto> MapShares(IReadOnlyList<Row> rows, int metricIndex)
        {
            var total = rows.Sum(r => MetricAt(r.Metrics, metricIndex));
            return rows
                .Select(r => new AnalyticsGeoRowDto
                {
                    Country = r.Dimensions.ElementAtOrDefault(0),
                    Users = MetricAt(r.Metrics, metricIndex),
                    Share = total > 0 ? Math.Round(MetricAt(r.Metrics, metricIndex) * 100d / total, 1) : 0d
                })
                .OrderByDescending(r => r.Users)
                .ToList();
        }

        private static int Metric(Row row, int index) => MetricAt(row.Metrics, index);

        private static int MetricAt(IReadOnlyList<string> metrics, int index)
        {
            if (index >= metrics.Count) return 0;
            return int.TryParse(metrics[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0d;
        }

        private async Task<List<Row>> RunReportAsync(
            HttpClient client,
            string accessToken,
            string propertyId,
            DateOnly startDate,
            DateOnly endDate,
            string[] dimensions,
            string[] metrics,
            CancellationToken cancellationToken,
            int? limit = null)
        {
            var payload = new Dictionary<string, object?>
            {
                ["dateRanges"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["startDate"] = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["endDate"] = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    }
                },
                ["dimensions"] = dimensions.Select(d => new Dictionary<string, object?> { ["name"] = d }).ToArray(),
                ["metrics"] = metrics.Select(m => new Dictionary<string, object?> { ["name"] = m }).ToArray(),
                ["keepEmptyRows"] = false
            };
            if (limit.HasValue) payload["limit"] = limit.Value;

            using var request = new HttpRequestMessage(HttpMethod.Post, string.Format(CultureInfo.InvariantCulture, ReportEndpoint, propertyId))
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("GA4 report failed with {Status}: {Body}", (int)response.StatusCode, body);
                throw new AnalyticsUnavailableException(
                    $"GA4 returned {(int)response.StatusCode}. The property may be missing or the service account may lack access.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<RunReportResponse>(json, JsonOptions);
            return parsed?.Rows ?? new List<Row>();
        }

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private async Task<string> GetAccessTokenAsync(AnalyticsOptions options, CancellationToken cancellationToken)
        {
            if (_accessToken is not null && _accessTokenExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            var assertion = CreateAssertion(options);
            var body = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion
            };

            var client = _httpClientFactory.CreateClient("ga4");
            using var response = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(body), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google token exchange failed: {Body}", text);
                throw new AnalyticsUnavailableException("The GA4 service account could not be authenticated.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var token = document.RootElement.TryGetProperty("access_token", out var tokenElement)
                ? tokenElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new AnalyticsUnavailableException("The GA4 service account did not return an access token.");
            }

            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expiresElement)
                && expiresElement.TryGetInt32(out var seconds)
                ? seconds
                : 3600;

            _accessToken = token;
            _accessTokenExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));
            return _accessToken;
        }

        /// <summary>
        /// Builds the RS256 assertion the Google token endpoint expects.
        /// The scope is embedded in the claim, so the request body stays empty.
        /// </summary>
        private static string CreateAssertion(AnalyticsOptions options)
        {
            using var rsa = RSA.Create();
            var key = options.PrivateKey.Replace("\\n", "\n");
            rsa.ImportFromPem(key);

            var now = DateTimeOffset.UtcNow;
            var header = new Dictionary<string, object> { ["alg"] = "RS256", ["typ"] = "JWT" };
            var payload = new Dictionary<string, object>
            {
                ["iss"] = options.ServiceAccountEmail,
                ["scope"] = Scope,
                ["aud"] = TokenEndpoint,
                ["iat"] = now.ToUnixTimeSeconds(),
                ["exp"] = now.AddMinutes(60).ToUnixTimeSeconds()
            };

            var headerSegment = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions));
            var payloadSegment = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
            var signature = rsa.SignData(
                Encoding.UTF8.GetBytes($"{headerSegment}.{payloadSegment}"),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return $"{headerSegment}.{payloadSegment}.{Base64Url(signature)}";
        }

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static List<AnalyticsDeviceRowDto> MapDevices(IReadOnlyList<Row> rows)
        {
            var total = rows.Sum(r => MetricAt(r.Metrics, 0));
            return rows
                .Select(r => new AnalyticsDeviceRowDto
                {
                    Category = r.Dimensions.ElementAtOrDefault(0),
                    Users = MetricAt(r.Metrics, 0),
                    Share = total > 0 ? Math.Round(MetricAt(r.Metrics, 0) * 100d / total, 1) : 0d
                })
                .OrderByDescending(r => r.Users)
                .ToList();
        }

        private sealed class RunReportResponse
        {
            [JsonPropertyName("rows")]
            public List<Row>? Rows { get; set; }
        }

        private sealed class Row
        {
            [JsonPropertyName("dimensionValues")]
            public List<DimensionValue>? DimensionValues { get; set; }

            [JsonPropertyName("metricValues")]
            public List<MetricValue>? MetricValues { get; set; }

            [JsonIgnore]
            public IReadOnlyList<string> Dimensions =>
                DimensionValues?.Select(d => d.Value ?? string.Empty).ToArray() ?? Array.Empty<string>();

            [JsonIgnore]
            public IReadOnlyList<string> Metrics =>
                MetricValues?.Select(m => m.Value ?? "0").ToArray() ?? Array.Empty<string>();
        }

        private sealed class DimensionValue
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }

        private sealed class MetricValue
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }
    }
}
