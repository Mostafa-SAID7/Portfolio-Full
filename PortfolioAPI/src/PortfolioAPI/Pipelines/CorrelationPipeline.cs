using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace PortfolioAPI.Pipelines
{
    /// <summary>
    /// Middleware for extracting, validating, and propagating correlation and distributed trace identifiers.
    /// Injects CorrelationId and TraceId into Serilog LogContext and ensures response headers carry X-Correlation-ID.
    /// </summary>
    public static partial class CorrelationPipeline
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemKey = "CorrelationId";

        // Allows alphanumeric characters, hyphens, underscores, and dots up to 64 chars.
        // Rejects carriage returns, newlines, control characters, and dangerous payload attempts.
        private static readonly Regex SafeIdRegex = new(@"^[a-zA-Z0-9_\-\.]{1,64}$", RegexOptions.Compiled);

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.Use(async (context, next) =>
            {
                var correlationId = ResolveOrGenerateCorrelationId(context);

                context.Items[ItemKey] = correlationId;

                // Propagate header in response
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(HeaderName))
                    {
                        context.Response.Headers[HeaderName] = correlationId;
                    }
                    return Task.CompletedTask;
                });

                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                var spanId = Activity.Current?.SpanId.ToString() ?? string.Empty;

                using (LogContext.PushProperty("CorrelationId", correlationId))
                using (LogContext.PushProperty("TraceId", traceId))
                using (LogContext.PushProperty("SpanId", spanId))
                {
                    await next();
                }
            });
        }

        public static string ResolveOrGenerateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
                !string.IsNullOrWhiteSpace(incoming))
            {
                var raw = incoming.ToString().Trim();
                if (SafeIdRegex.IsMatch(raw))
                {
                    return raw;
                }
            }

            // Fallback: use current W3C activity trace ID or generate a compact unique ID
            return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }
    }
}

