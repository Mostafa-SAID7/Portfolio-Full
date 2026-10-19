using System.Collections.Generic;
using System.Linq;

namespace PortfolioAPI.Core.Common
{
    /// <summary>
    /// Lightweight result wrapper used by the service layer so operations can
    /// fail with a message (and optional field errors) without throwing for
    /// expected outcomes. Unexpected failures still surface as exceptions and
    /// are handled by the API error middleware.
    /// </summary>
    public class Result<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        /// <summary>
        /// True when the failure means "the entity does not exist" — mapped to
        /// HTTP 404 by the shared controller helper (issue #16). Other failures
        /// map to 400 with the shared { message, errors? } body.
        /// </summary>
        public bool IsNotFound { get; set; }

        public static Result<T> Ok(T data, string? message = null)
        {
            return new Result<T> { Success = true, Message = message, Data = data };
        }

        public static Result<T> Fail(string message, IEnumerable<string>? errors = null)
        {
            return new Result<T> { Success = false, Message = message, Errors = errors };
        }

        /// <summary>Expected "not found" outcome — surfaced as HTTP 404 { message }.</summary>
        public static Result<T> NotFound(string message)
        {
            return new Result<T> { Success = false, IsNotFound = true, Message = message };
        }

        public static Result<T> Fail(IEnumerable<string> errors)
        {
            return new Result<T>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = errors.ToArray()
            };
        }
    }
}
