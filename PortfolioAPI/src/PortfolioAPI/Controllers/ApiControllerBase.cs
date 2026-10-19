using Microsoft.AspNetCore.Mvc;
using PortfolioAPI.Core.Common;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Base controller with the shared Result&lt;T&gt; → HTTP mapping (issue #16),
    /// so every admin controller returns the same status codes and the same
    /// { message, errors? } JSON error shape as the exception middleware.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Success → 200 with the payload; not-found → 404 { message };
        /// other failure → 400 { message, errors? }.
        /// </summary>
        protected ObjectResult Mapped<T>(Result<T> result)
        {
            var (status, body) = result.ToHttp();
            return new ObjectResult(body) { StatusCode = status };
        }

        /// <summary>
        /// Error-only mapping for endpoints that build their own success
        /// response (NoContent / CreatedAtAction / Ok with extra data).
        /// </summary>
        protected ObjectResult Failure<T>(Result<T> result)
        {
            var (status, body) = result.ToHttpError();
            return new ObjectResult(body) { StatusCode = status };
        }
    }
}
