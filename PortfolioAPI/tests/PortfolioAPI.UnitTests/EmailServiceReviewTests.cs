using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PortfolioAPI.Infrastructure.Emailing;
using PortfolioAPI.Infrastructure.Services;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// Locks the EmailService guard rails: placeholder detection, connection
    /// fail-fast messages, SMTP subject sanitization (a visitor pressing Enter
    /// in the subject field must not break the send) and the strongly-typed
    /// settings binding that survives partial configuration.
    /// </summary>
    public class EmailServiceReviewTests
    {
        private static EmailSettingsOptions ValidSettings() => new()
        {
            SmtpServer = "smtp.gmail.com",
            SmtpPort = 587,
            Username = "portfolio@example.com",
            Password = "app-password",
            FromEmail = "portfolio@example.com",
            FromName = "Portfolio"
        };

        private static EmailService CreateService(EmailSettingsOptions settings)
        {
            var configuration = new ConfigurationBuilder().Build();
            return new EmailService(Options.Create(settings), configuration);
        }

        [Fact]
        public void PlaceholderDetection_CoversExactAndEmptyValues()
        {
            EmailSettingsOptions.IsPlaceholder("your-email@gmail.com").Should().BeTrue();
            EmailSettingsOptions.IsPlaceholder("your-password-here").Should().BeTrue();
            EmailSettingsOptions.IsPlaceholder("YOUR-PASSWORD").Should().BeTrue();
            EmailSettingsOptions.IsPlaceholder("").Should().BeTrue();
            EmailSettingsOptions.IsPlaceholder("   ").Should().BeTrue();
            EmailSettingsOptions.IsPlaceholder(null).Should().BeTrue();

            EmailSettingsOptions.IsPlaceholder("real-credentials").Should().BeFalse();
        }

        [Fact]
        public void ValidateConnectionSettings_PassesForCompleteRealCredentials()
        {
            var act = () => CreateService(ValidSettings()).ValidateConnectionSettings();

            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateConnectionSettings_FailsFastOnMissingHostOrSender()
        {
            var settings = ValidSettings();
            settings.SmtpServer = "";

            var act = () => CreateService(settings).ValidateConnectionSettings();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("EmailSettings:SmtpServer and EmailSettings:FromEmail are not configured.");
        }

        [Fact]
        public void ValidateConnectionSettings_FailsFastOnShippedPlaceholders()
        {
            var settings = ValidSettings();
            settings.Password = "your-password-here";

            var act = () => CreateService(settings).ValidateConnectionSettings();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*still hold the shipped placeholders*");
        }

        [Fact]
        public void SanitizeSubject_StripsLineBreaksThatWouldBreakSmtpHeaders()
        {
            EmailService.SanitizeSubject("First line\r\nBCC: attacker@example.com")
                .Should().Be("First line BCC: attacker@example.com");

            EmailService.SanitizeSubject("multi\nline\r\nsubject")
                .Should().Be("multi line subject");
        }

        [Fact]
        public void SanitizeSubject_ReturnsFallbackForEmptySubject()
        {
            EmailService.SanitizeSubject("").Should().Be("(no subject)");
            EmailService.SanitizeSubject("   ").Should().Be("(no subject)");
        }

        [Fact]
        public void EmailSettingsOptions_BindsPartialConfigAndKeepsPortDefault()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "smtp.gmail.com"
                })
                .Build();

            var options = configuration.GetSection(EmailSettingsOptions.SectionName)
                .Get<EmailSettingsOptions>();

            options.Should().NotBeNull();
            options!.SmtpServer.Should().Be("smtp.gmail.com");
            options.SmtpPort.Should().Be(587); // documented default survives partial config
            options.FromName.Should().Be("Portfolio API"); // documented default survives partial config
        }
    }
}
