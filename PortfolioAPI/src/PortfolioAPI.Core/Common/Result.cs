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

        public static Result<T> Ok(T data, string? message = null)
        {
            return new Result<T> { Success = true, Message = message, Data = data };
        }

        public static Result<T> Fail(string message, IEnumerable<string>? errors = null)
        {
            return new Result<T> { Success = false, Message = message, Errors = errors };
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
