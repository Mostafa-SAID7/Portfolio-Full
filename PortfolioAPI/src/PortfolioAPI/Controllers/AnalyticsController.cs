using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Exceptions;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// GA4 reports for the private analytics dashboard.
    ///
    /// Admin-only: the browser never sees a Google credential, and the
    /// dashboard can only reach these endpoints with a valid admin JWT.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        /// <summary>Longest period the Data API is queried for in one request.</summary>
        private const int MaxRangeDays = 90;

        private readonly IAnalyticsReportingService _analytics;

        public AnalyticsController(IAnalyticsReportingService analytics)
        {
            _analytics = analytics;
        }

        /// <summary>
        /// The full dashboard payload: overview, trend, countries, sources,
        /// pages, devices and tracked events for one period.
        /// </summary>
        [HttpGet("report")]
        public async Task<ActionResult<AnalyticsReportDto>> GetReport(
            [FromQuery] string? start,
            [FromQuery] string? end,
            CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var (startDate, endDate) = ResolveRange(start, end, today);

            try
            {
                var report = await _analytics.GetReportAsync(startDate, endDate, cancellationToken);
                return Ok(report);
            }
            catch (AnalyticsNotConfiguredException ex)
            {
                // 501: the feature is understood but not configured for this deployment.
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
            }
            catch (AnalyticsUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
        }

        /// <summary>Whether GA4 is configured at all (cheap check for the UI).</summary>
        [HttpGet("status")]
        public ActionResult<object> GetStatus()
        {
            return Ok(new
            {
                configured = _analytics.IsConfigured,
                maxRangeDays = MaxRangeDays
            });
        }

        /// <summary>
        /// Validates the requested period: start ≤ end, not in the future and
        /// not longer than the allowed window.
        /// </summary>
        private (DateOnly Start, DateOnly End) ResolveRange(string? start, string? end, DateOnly today)
        {
            var endDate = ParseDate(end) ?? today;
            var startDate = ParseDate(start) ?? endDate.AddDays(-27);

            if (startDate > endDate)
            {
                throw new ArgumentException("The start date must not be after the end date.");
            }
            if (endDate > today)
            {
                throw new ArgumentException("The end date must not be in the future.");
            }
            var days = endDate.DayNumber - startDate.DayNumber + 1;
            if (days > MaxRangeDays)
            {
                throw new ArgumentException($"The period must not exceed {MaxRangeDays} days.");
            }

            return (startDate, endDate);
        }

        private static DateOnly? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : null;
        }
    }
}
