using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Exceptions;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Pipelines
{
    /// <summary>
    /// Central exception handling middleware.
    /// Provides consistent JSON error responses and authoritative, structured exception logging
    /// without duplicate records, leaking stack traces, or misclassifying client errors as system failures.
    /// </summary>
    public static class ErrorHandlingPipeline
    {
        public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PortfolioAPI.ExceptionHandler");
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? string.Empty;

                int status;
                string message;
                IDictionary<string, string[]>? errors = null;

                switch (exception)
                {
                    case NotFoundException notFound:
                        status = StatusCodes.Status404NotFound;
                        message = notFound.Message;
                        logger.LogInformation("Resource not found for {Method} {Path}: {Message}", method, path, message);
                        break;

                    case ValidationException validation:
                        status = StatusCodes.Status400BadRequest;
                        message = validation.Message;
                        errors = validation.Errors;
                        logger.LogWarning("Validation failed for {Method} {Path}: {Message}. Errors: {@ValidationErrors}", method, path, message, errors);
                        break;

                    case EmailDeliveryException delivery:
                        status = StatusCodes.Status503ServiceUnavailable;
                        message = delivery.Message;
                        logger.LogWarning("External email delivery failure for {Method} {Path}: {Message}", method, path, message);
                        break;

                    case AnalyticsNotConfiguredException analyticsNotConfigured:
                        status = StatusCodes.Status501NotImplemented;
                        message = analyticsNotConfigured.Message;
                        logger.LogInformation("Analytics requested but not configured on this host for {Method} {Path}", method, path);
                        break;

                    case AnalyticsUnavailableException analyticsUnavailable:
                        status = StatusCodes.Status503ServiceUnavailable;
                        message = analyticsUnavailable.Message;
                        logger.LogWarning("Analytics service unavailable for {Method} {Path}: {Message}", method, path, message);
                        break;

                    case DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 } } dbConflict:
                        status = StatusCodes.Status409Conflict;
                        message = "That name is already taken — choose a different one.";
                        logger.LogWarning(dbConflict, "Database unique constraint conflict for {Method} {Path}", method, path);
                        break;

                    case DbUpdateException dbUpdateEx:
                        status = StatusCodes.Status500InternalServerError;
                        message = "A database error occurred while updating the entity.";
                        logger.LogError(dbUpdateEx, "Database update operation failed for {Method} {Path}", method, path);
                        break;

                    case OperationCanceledException:
                        logger.LogInformation("Request was cancelled by client for {Method} {Path}", method, path);
                        return;

                    default:
                        status = StatusCodes.Status500InternalServerError;
                        message = "An unexpected error occurred while processing the request.";
                        logger.LogError(exception, "Unhandled server error processing {Method} {Path}", method, path);
                        break;
                }

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                if (errors is null)
                {
                    await context.Response.WriteAsJsonAsync(new { message });
                }
                else
                {
                    await context.Response.WriteAsJsonAsync(new { message, errors });
                }
            }));

            return app;
        }
    }
}
