// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

namespace Quick_Application1.Core.Services.Portfolio.Exceptions
{
    public class PortfolioException : Exception
    {
        public PortfolioException()
        {
        }

        public PortfolioException(string message)
            : base(message)
        {
        }

        public PortfolioException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}