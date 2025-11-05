using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace PortfolioAPI.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            using var smtpClient = new SmtpClient
            {
                Host = smtpSettings["Host"] ?? throw new InvalidOperationException("SMTP Host not configured"),
                Port = int.Parse(smtpSettings["Port"] ?? "587"),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"]
                )
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"] ?? throw new InvalidOperationException("From email not configured")),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message)
        {
            var adminEmail = _configuration["AdminEmail"] ?? throw new InvalidOperationException("Admin email not configured");

            var emailBody = $@"
                <h2>New Contact Form Submission</h2>
                <p><strong>From:</strong> {fromName} ({fromEmail})</p>
                <p><strong>Subject:</strong> {subject}</p>
                <p><strong>Message:</strong></p>
                <p>{message.Replace("\n", "<br>")}</p>
                <hr>
                <p><em>This message was sent from the portfolio contact form.</em></p>
            ";

            await SendEmailAsync(adminEmail, $"Portfolio Contact: {subject}", emailBody, true);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to My Portfolio!";
            var body = $@"
                <h2>Welcome {userName}!</h2>
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
                <h2>{title}</h2>
                <p>{content}</p>
                <hr>
                <p><em>This is an automated notification from the portfolio system.</em></p>
            ";

            await SendEmailAsync(toEmail, subject, body, true);
        }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task SendContactFormEmailAsync(string fromName, string fromEmail, string subject, string message);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendNotificationEmailAsync(string toEmail, string title, string content);
    }
}