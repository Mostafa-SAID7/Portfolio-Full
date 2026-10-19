namespace PortfolioAPI.Core.Common
{
    /// <summary>
    /// Shared Result&lt;T&gt; → HTTP mapping (issue #16). One place defines the
    /// status codes and the JSON error shape every controller uses:
    ///
    ///   200 → the payload (Data)
    ///   204 → success with no payload (Data == null)
    ///   404 → { "message": "..." }                (Result.NotFound)
    ///   400 → { "message": "..." }                (other failure)
    ///
    /// This matches the API's exception middleware, which emits the same
    /// { message } shape for NotFoundException / ValidationException
    /// (validation additionally carries the field-keyed `errors` object).
    /// </summary>
    public static class ResultExtensions
    {
        public static (int StatusCode, object Body) ToHttp<T>(this Result<T> result)
        {
            if (result.Success)
            {
                // A successful result without a payload is a 204, never an error.
                return result.Data is null
                    ? (204, (object)Array.Empty<byte>())
                    : (200, result.Data);
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
            return (400, (object)new { message });
        }
    }
}
