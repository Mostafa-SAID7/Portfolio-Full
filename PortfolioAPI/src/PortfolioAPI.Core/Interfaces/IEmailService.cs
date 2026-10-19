namespace PortfolioAPI.Core.Interfaces
{
    /// <summary>
    /// Outbound email port (SOLID: the contract lives in Core, the SMTP
    /// adapter in Infrastructure implements it — the application layer can
    /// depend on the port without referencing the infrastructure assembly).
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendNotificationEmailAsync(string toEmail, string title, string content);
    }
}
