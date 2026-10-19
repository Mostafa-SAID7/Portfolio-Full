using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using PortfolioAPI.Core.Interfaces;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// SMTP adapter for <see cref="IEmailService"/>. Reads the tracked
    /// `EmailSettings` section (SmtpServer / SmtpPort / Username / Password /
    /// FromEmail / AdminEmail) — the previous implementation read a
    /// `SmtpSettings` section that exists in no config file, so the first real
    /// send would have thrown. Unconfigured or placeholder credentials fail
    /// with a clear InvalidOperationException; ContactService catches that and
    /// logs a warning without ever failing the visitor's submission.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var settings = _configuration.GetSection("EmailSettings");
            var host = settings["SmtpServer"];
            var fromEmail = settings["FromEmail"];
            var username = settings["Username"];
            var password = settings["Password"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("EmailSettings:SmtpServer and EmailSettings:FromEmail are not configured.");
            }
            if (IsPlaceholder(username) || IsPlaceholder(password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:Username/Password still hold the shipped placeholders — configure real SMTP credentials.");
            }

            using var smtpClient = new SmtpClient
            {
                Host = host,
                Port = int.TryParse(settings["SmtpPort"], out var port) ? port : 587,
                EnableSsl = true,
                // Bounded timeout: a hung SMTP server must not pin the request
                // thread for the 100 s SmtpClient default.
                Timeout = 15000
            };
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, settings["FromName"] ?? "Portfolio API"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message)
        {
            // Admin recipient: explicit setting first, then the Auth profile's
            // address (already maintained in production config).
            var adminEmail = _configuration["EmailSettings:AdminEmail"] ?? _configuration["Auth:Email"];
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException("No admin recipient configured (EmailSettings:AdminEmail).");
            }

            // Every user-supplied value is HTML-encoded before it lands in the
            // admin mailbox — a contact message must never inject markup.
            var emailBody = $@"
                <h2>New Contact Form Submission</h2>
                <p><strong>From:</strong> {WebUtility.HtmlEncode(fromName)} ({WebUtility.HtmlEncode(fromEmail)})</p>
                <p><strong>Subject:</strong> {WebUtility.HtmlEncode(subject)}</p>
                <p><strong>Message:</strong></p>
                <p>{WebUtility.HtmlEncode(message).Replace("\n", "<br>")}</p>
                <hr>
                <p><em>This message was sent from the portfolio contact form.</em></p>
            ";

            await SendEmailAsync(adminEmail, $"Portfolio Contact: {subject}", emailBody, true);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to My Portfolio!";
            var body = $@"
                <h2>Welcome {WebUtility.HtmlEncode(userName)}!</h2>
                <p>Thank you for visiting my portfolio. I'm excited to share my work and experience with you.</p>
                <p>Feel free to explore the projects and get in touch if you'd like to discuss potential opportunities.</p>
                <br>
                <p>Best regards,<br>Your Portfolio Owner</p>
            ";

            await SendEmailAsync(toEmail, subject, body, true);
        }

        public async Task SendNotificationEmailAsync(string toEmail, string title, string content)
        {
            var subject = $"Portfolio Notification: {title}";
            var body = $@"
                <h2>{WebUtility.HtmlEncode(title)}</h2>
                <p>{WebUtility.HtmlEncode(content)}</p>
                <hr>
                <p><em>This is an automated notification from the portfolio system.</em></p>
            ";

            await SendEmailAsync(toEmail, subject, body, true);
        }

        /// <summary>True for the placeholder values shipped in appsettings.json.</summary>
        private static bool IsPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value)
            || value.StartsWith("your-", StringComparison.OrdinalIgnoreCase);
    }
}
