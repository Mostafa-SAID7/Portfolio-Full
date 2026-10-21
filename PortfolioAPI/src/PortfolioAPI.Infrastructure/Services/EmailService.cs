using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Emailing;

namespace PortfolioAPI.Infrastructure.Services
{
    /// <summary>
    /// SMTP adapter for <see cref="IEmailService"/>.
    ///
    /// Settings are bound once to <see cref="EmailSettingsOptions"/> (the same
    /// Options pattern the storage settings use) instead of per-call magic
    /// strings. Unconfigured or placeholder credentials fail with a clear
    /// InvalidOperationException; ContactService catches that and logs a
    /// warning without ever failing the visitor's submission.
    ///
    /// HTML bodies come from the embedded Emailing/Templates files with every
    /// user-supplied value HTML-encoded BEFORE substitution (see
    /// EmailTemplateRenderer for the renderer contract).
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsOptions _settings;
        private readonly IConfiguration _configuration;

        public EmailService(IOptions<EmailSettingsOptions> options, IConfiguration configuration)
        {
            _settings = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            ValidateConnectionSettings();

            using var smtpClient = new SmtpClient
            {
                Host = _settings.SmtpServer,
                Port = _settings.SmtpPort,
                EnableSsl = true,
                // Bounded timeout: a hung SMTP server must not pin the request
                // thread for the 100 s SmtpClient default.
                Timeout = 15000
            };
            if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                // Google shows app passwords in groups of four ("abcd efgh ijkl mnop");
                // SMTP auth needs them unspaced. Stripping whitespace is safe
                // for the providers we target and saves a confusing 535 error.
                smtpClient.Credentials = new NetworkCredential(
                    _settings.Username.Trim(),
                    _settings.Password.Replace(" ", string.Empty).Trim());
            }

            using var mailMessage = BuildMessage(to, subject, body, isHtml);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message)
        {
            // Admin recipient: explicit setting first, then the Auth profile's
            // address (already maintained in production config).
            var adminEmail = FirstNonEmpty(_settings.AdminEmail, _configuration["Auth:Email"]);
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException("No admin recipient configured (EmailSettings:AdminEmail).");
            }

            // Every user-supplied value is HTML-encoded BEFORE it reaches the
            // template — a contact message must never inject markup. (The
            // renderer itself is mechanical; callers own encoding.)
            var emailBody = EmailTemplateRenderer.Render(
                "ContactFormSubmission.html",
                new Dictionary<string, string>
                {
                    ["FromName"] = WebUtility.HtmlEncode(fromName),
                    ["FromEmail"] = WebUtility.HtmlEncode(fromEmail),
                    ["Subject"] = WebUtility.HtmlEncode(subject),
                    // Line breaks inside the message must survive visually as
                    // <br>, so the encoded text is converted here (the ONLY
                    // place where post-encoding markup is added).
                    ["MessageHtml"] = WebUtility.HtmlEncode(message).Replace("\n", "<br />")
                });

            await SendEmailAsync(adminEmail, $"Portfolio Contact: {subject}", emailBody, true);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to My Portfolio!";
            var body = EmailTemplateRenderer.Render(
                "Welcome.html",
                new Dictionary<string, string>
                {
                    ["UserName"] = WebUtility.HtmlEncode(userName)
                });

            await SendEmailAsync(toEmail, subject, body, true);
        }

        public async Task SendNotificationEmailAsync(string toEmail, string title, string content)
        {
            var subject = $"Portfolio Notification: {title}";
            var body = EmailTemplateRenderer.Render(
                "Notification.html",
                new Dictionary<string, string>
                {
                    ["Title"] = WebUtility.HtmlEncode(title),
                    ["Content"] = WebUtility.HtmlEncode(content)
                });

            await SendEmailAsync(toEmail, subject, body, true);
        }

        /// <summary>
        /// Fail fast with clear, actionable messages when the SMTP connection
        /// settings cannot possibly work — instead of a cryptic socket error
        /// deep inside SmtpClient.
        /// </summary>
        internal void ValidateConnectionSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpServer) || string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                throw new InvalidOperationException("EmailSettings:SmtpServer and EmailSettings:FromEmail are not configured.");
            }
            if (EmailSettingsOptions.IsPlaceholder(_settings.Username) || EmailSettingsOptions.IsPlaceholder(_settings.Password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:Username/Password still hold the shipped placeholders — configure real SMTP credentials.");
            }
        }

        /// <summary>
        /// Validates the addresses as early as possible and builds the message.
        /// A malformed recipient fails here with a clear reason instead of a
        /// generic FormatException from deep inside SmtpClient.
        /// </summary>
        private MailMessage BuildMessage(string to, string subject, string body, bool isHtml)
        {
            if (string.IsNullOrWhiteSpace(to) || !MailAddress.TryCreate(to.Trim(), out _))
            {
                // The recipient address is deliberately NOT echoed back into the
                // exception message.
                throw new InvalidOperationException("The email recipient address is not valid.");
            }
            if (!MailAddress.TryCreate(_settings.FromEmail.Trim(), out var from))
            {
                throw new InvalidOperationException("EmailSettings:FromEmail is not a valid SMTP address.");
            }

            return new MailMessage
            {
                From = new MailAddress(from.Address, string.IsNullOrWhiteSpace(_settings.FromName)
                    ? "Portfolio API"
                    : _settings.FromName),
                Subject = SanitizeSubject(subject),
                Body = body,
                IsBodyHtml = isHtml
            };
        }

        /// <summary>
        /// A visitor-supplied subject goes straight into the SMTP headers. CR/LF
        /// are normalized to a single space so a multi-line subject can never
        /// break the header block (SmtpClient refuses line breaks in Subject and
        /// the send dies with a cryptic exception at exactly the worst moment —
        /// a real visitor waiting on the contact form).
        /// </summary>
        internal static string SanitizeSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                return "(no subject)";
            }

            var flattened = string.Join(" ",
                subject
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Trim())
                    .Where(part => part.Length > 0));

            return flattened.Length == 0 ? "(no subject)" : flattened;
        }

        private static string FirstNonEmpty(string? first, string? second) =>
            !string.IsNullOrWhiteSpace(first) ? first : (second ?? string.Empty);
    }
}
