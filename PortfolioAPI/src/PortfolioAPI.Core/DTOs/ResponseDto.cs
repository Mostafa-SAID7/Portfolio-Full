namespace PortfolioAPI.Core.DTOs
{
    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ResponseDto<T> SuccessResponse(T data, string? message = null)
        {
            return new ResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ResponseDto<T> ErrorResponse(string message, IEnumerable<string>? errors = null)
        {
            return new ResponseDto<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}