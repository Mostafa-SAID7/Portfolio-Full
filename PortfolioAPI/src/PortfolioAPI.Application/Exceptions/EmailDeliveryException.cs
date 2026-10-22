namespace PortfolioAPI.Application.Exceptions
{
    /// <summary>
    /// The message was valid but outbound mail could not deliver it (unconfigured
    /// or failing SMTP). Surfaced as 503 so the dashboard can say "not sent"
    /// instead of blaming the admin's text — and so a reply is never recorded
    /// as delivered.
    /// </summary>
    public class EmailDeliveryException : Exception
    {
        public EmailDeliveryException(string message) : base(message)
        {
        }

        public EmailDeliveryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
