using System.Collections.Generic;
using System.Linq;

namespace PortfolioAPI.Core.Common
{
    /// <summary>
    /// Shared Result&lt;T&gt; → HTTP mapping (issue #16). One place defines the
    /// status codes and the consistent JSON error shape every controller uses:
    ///
    ///   200 → the payload (Data)
    ///   404 → { "message": "..." }                (Result.NotFound)
    ///   400 → { "message": "...", "errors": [...] } (validation/other failure)
    ///
    /// This matches the API's exception middleware, which emits the same
    /// { message, errors? } shape for NotFoundException / ValidationException.
    /// </summary>
    public static class ResultExtensions
    {
        public static (int StatusCode, object Body) ToHttp<T>(this Result<T> result)
        {
            if (result.Success && result.Data is not null)
            {
                return (200, result.Data);
            }

            return result.ToHttpError();
        }

        public static (int StatusCode, object Body) ToHttpError<T>(this Result<T> result)
        {
            if (result.IsNotFound)
            {
                return (404, (object)new { message = result.Message ?? "The requested resource was not found." });
            }

            var message = result.Message ?? "The request could not be processed.";
            return result.Errors != null && result.Errors.Any()
                ? (400, (object)new { message, errors = result.Errors.ToArray() })
                : (400, (object)new { message });
        }
    }
}
