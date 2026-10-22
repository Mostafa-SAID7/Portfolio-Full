using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Application.Exceptions;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Pipelines
{
    /// <summary>
    /// Central exception handling — extracted from Program.cs.
    /// </summary>
    public static class ErrorHandlingPipeline
    {
        /// <summary>
        /// Error handling at the TOP so faults anywhere below — CORS, static
        /// uploads, Swagger, controllers — get the shared JSON shape instead of a
        /// blank 500. Known domain exceptions get a proper status + a REAL
        /// message (the dashboards render `message` verbatim); everything else
        /// stays a generic 500 so production never leaks stack traces. Before
        /// this mapping, a missing entity or a FluentValidation failure from any
        /// service surfaced as a 500 "An unexpected error occurred..." —
        /// validation messages were silently lost.
        /// </summary>
        public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                int status;
                string message;
                IDictionary<string, string[]>? errors = null;

                switch (exception)
                {
                    case NotFoundException notFound:
                        status = StatusCodes.Status404NotFound;
                        message = notFound.Message;
                        break;
                    case ValidationException validation:
                        status = StatusCodes.Status400BadRequest;
                        message = validation.Message;
                        errors = validation.Errors;
                        break;
                    // Outbound mail is an upstream dependency: an unconfigured or
                    // failing SMTP must read as "could not be sent", never as a
                    // generic 500 and never as a saved reply.
                    case EmailDeliveryException delivery:
                        status = StatusCodes.Status503ServiceUnavailable;
                        message = delivery.Message;
                        break;
                    // Unique indexes (Skills.Name) reject duplicates at the
                    // database level — surface them as a clear 409 instead of a generic 500.
                    case DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 } }:
                        status = StatusCodes.Status409Conflict;
                        message = "That name is already taken — choose a different one.";
                        break;
                    default:
                        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PortfolioAPI.ExceptionHandler");
                        logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                        status = StatusCodes.Status500InternalServerError;
                        message = "An unexpected error occurred while processing the request.";
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
