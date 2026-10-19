namespace PortfolioAPI.Core.Common
{
    /// <summary>
    /// Lightweight result wrapper used by the service layer so operations can
    /// fail with a message without throwing for expected outcomes. Expected
    /// *validation* failures and unexpected faults still surface as exceptions
    /// and are handled by the API exception middleware (the single authority
    /// on the { message, errors? } error shape).
    /// </summary>
    public class Result<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        /// <summary>
        /// True when the failure means "the entity does not exist" — mapped to
        /// HTTP 404 by the shared controller helper (issue #16). Other failures
        /// map to 400 with the shared { message } body.
        /// </summary>
        public bool IsNotFound { get; set; }

        public static Result<T> Ok(T data, string? message = null)
        {
            return new Result<T> { Success = true, Message = message, Data = data };
        }

        /// <summary>Expected "not found" outcome — surfaced as HTTP 404 { message }.</summary>
        public static Result<T> NotFound(string message)
        {
            return new Result<T> { Success = false, IsNotFound = true, Message = message };
        }
    }
}
