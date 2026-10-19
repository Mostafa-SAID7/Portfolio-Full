using System.Net;
using Bogus;
using FluentAssertions;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class EmailServiceIntegrationTests : TestBase
    {
        private readonly Faker<Contact> _contactFaker;

        public EmailServiceIntegrationTests()
        {
            _contactFaker = new Faker<Contact>()
                .RuleFor(c => c.Name, f => f.Name.FullName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.Subject, f => f.Lorem.Sentence(5))
                .RuleFor(c => c.Message, f => f.Lorem.Paragraphs(2));
        }

        [Fact]
        public async Task SendEmailAsync_SendsEmailSuccessfully()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var toEmail = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await emailService.SendEmailAsync(toEmail, subject, body);

            // Assert
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.To.Should().Be(toEmail);
            sentEmail.Subject.Should().Be(subject);
            sentEmail.Body.Should().Be(body);
            sentEmail.IsHtml.Should().BeFalse();
            sentEmail.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SendEmailAsync_SendsHtmlEmailSuccessfully()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var toEmail = "test@example.com";
            var subject = "Test HTML Subject";
            var htmlBody = "<h1>Test HTML Body</h1><p>This is a test email.</p>";

            // Act
            await emailService.SendEmailAsync(toEmail, subject, htmlBody, true);

            // Assert
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.To.Should().Be(toEmail);
            sentEmail.Subject.Should().Be(subject);
            sentEmail.Body.Should().Be(htmlBody);
            sentEmail.IsHtml.Should().BeTrue();
        }

        [Fact]
        public async Task SendContactFormEmailAsync_SendsFormattedEmail()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contact = _contactFaker.Generate();

            // Act
            await emailService.SendContactFormEmailAsync(
                contact.Name,
                contact.Email,
                contact.Subject ?? "No Subject",
                contact.Message);

            // Assert
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.To.Should().Be("admin@test.com"); // Default admin email from test config
            sentEmail.Subject.Should().Be($"Portfolio Contact: {contact.Subject ?? "No Subject"}");
            sentEmail.Body.Should().Contain("New Contact Form Submission");
            sentEmail.Body.Should().Contain($"From: {contact.Name} ({contact.Email})");
            sentEmail.Body.Should().Contain($"Subject: {contact.Subject ?? "No Subject"}");
            sentEmail.Body.Should().Contain("Message:");
            sentEmail.Body.Should().Contain(contact.Message.Replace("\n", "<br>"));
            sentEmail.IsHtml.Should().BeTrue();
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_SendsWelcomeEmail()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var userEmail = "newuser@example.com";
            var userName = "John Doe";

            // Act
            await emailService.SendWelcomeEmailAsync(userEmail, userName);

            // Assert
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.To.Should().Be(userEmail);
            sentEmail.Subject.Should().Be("Welcome to My Portfolio!");
            sentEmail.Body.Should().Contain($"Welcome {userName}!");
            sentEmail.Body.Should().Contain("Thank you for visiting my portfolio");
            sentEmail.Body.Should().Contain("Feel free to explore the projects");
            sentEmail.IsHtml.Should().BeTrue();
        }

        [Fact]
        public async Task SendNotificationEmailAsync_SendsNotificationEmail()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var userEmail = "user@example.com";
            var title = "Important Update";
            var content = "This is an important notification about your portfolio.";

            // Act
            await emailService.SendNotificationEmailAsync(userEmail, title, content);

            // Assert
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.To.Should().Be(userEmail);
            sentEmail.Subject.Should().Be($"Portfolio Notification: {title}");
            sentEmail.Body.Should().Contain(title);
            sentEmail.Body.Should().Contain(content);
            sentEmail.Body.Should().Contain("This is an automated notification");
            sentEmail.IsHtml.Should().BeTrue();
        }

        [Fact]
        public async Task EmailService_HandlesMultipleEmailsCorrectly()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contacts = _contactFaker.Generate(5);

            // Act - Send multiple emails
            foreach (var contact in contacts)
            {
                await emailService.SendContactFormEmailAsync(
                    contact.Name,
                    contact.Email,
                    contact.Subject ?? "Test Subject",
                    contact.Message);
            }

            await emailService.SendWelcomeEmailAsync("welcome@example.com", "Welcome User");
            await emailService.SendNotificationEmailAsync("notify@example.com", "Test Notification", "Test content");

            // Assert
            emailService.SentEmails.Should().HaveCount(7); // 5 contact + 1 welcome + 1 notification

            var contactEmails = emailService.SentEmails.Take(5).ToList();
            var welcomeEmail = emailService.SentEmails.Skip(5).First();
            var notificationEmail = emailService.SentEmails.Last();

            // Verify contact emails
            contactEmails.Should().AllSatisfy(email =>
            {
                email.Subject.Should().StartWith("Portfolio Contact:");
                email.Body.Should().Contain("New Contact Form Submission");
                email.IsHtml.Should().BeTrue();
            });

            // Verify welcome email
            welcomeEmail.Subject.Should().Be("Welcome to My Portfolio!");
            welcomeEmail.To.Should().Be("welcome@example.com");

            // Verify notification email
            notificationEmail.Subject.Should().Be("Portfolio Notification: Test Notification");
            notificationEmail.To.Should().Be("notify@example.com");
        }

        [Fact]
        public async Task EmailService_EmailContentFormatting_IsCorrect()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contact = _contactFaker.Generate();
            contact.Message = "Line 1\nLine 2\n\nLine 4"; // Test line break handling

            // Act
            await emailService.SendContactFormEmailAsync(
                contact.Name,
                contact.Email,
                contact.Subject ?? "Test",
                contact.Message);

            // Assert
            var sentEmail = emailService.SentEmails.First();
            sentEmail.Body.Should().Contain("Line 1<br>Line 2<br><br>Line 4"); // Line breaks converted to <br>
        }

        [Fact]
        public async Task EmailService_IntegrationWithContactApi_EndToEnd()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contact = _contactFaker.Generate();

            // Act - Create contact through API (which should send email)
            var response = await PostAsync("api/contact", contact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify email was sent through the service
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.Subject.Should().Contain("Contact");
            sentEmail.Body.Should().Contain(contact.Name);
            sentEmail.Body.Should().Contain(contact.Email);
            sentEmail.Body.Should().Contain(contact.Message);
        }

        [Fact]
        public async Task EmailService_ErrorHandling_DoesNotThrowExceptions()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();

            // Act & Assert - These should not throw exceptions in test environment
            await emailService.Invoking(es => es.SendEmailAsync("", "subject", "body"))
                .Should().NotThrowAsync();

            await emailService.Invoking(es => es.SendContactFormEmailAsync("", "", "", ""))
                .Should().NotThrowAsync();

            await emailService.Invoking(es => es.SendWelcomeEmailAsync("", ""))
                .Should().NotThrowAsync();

            await emailService.Invoking(es => es.SendNotificationEmailAsync("", "", ""))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task EmailService_EmailQueueing_SimulatesRealWorldUsage()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contacts = _contactFaker.Generate(10);

            // Act - Simulate high volume contact form submissions
            var emailTasks = contacts.Select(contact =>
                emailService.SendContactFormEmailAsync(
                    contact.Name,
                    contact.Email,
                    contact.Subject ?? "Bulk Test",
                    contact.Message));

            await Task.WhenAll(emailTasks);

            // Assert
            emailService.SentEmails.Should().HaveCount(10);

            // Verify all emails have unique timestamps (simulating real queueing)
            var timestamps = emailService.SentEmails.Select(e => e.SentAt).ToList();
            timestamps.Should().HaveCount(10);
            timestamps.Distinct().Should().HaveCount(10); // All timestamps should be unique
        }

        [Fact]
        public async Task EmailService_ContentValidation_HandlesEdgeCases()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();

            // Test with special characters
            var contactWithSpecialChars = _contactFaker.Generate();
            contactWithSpecialChars.Message = "Special chars: <>&\"'";
            contactWithSpecialChars.Subject = "Subject with <tags>";

            // Act
            await emailService.SendContactFormEmailAsync(
                contactWithSpecialChars.Name,
                contactWithSpecialChars.Email,
                contactWithSpecialChars.Subject,
                contactWithSpecialChars.Message);

            // Assert
            var sentEmail = emailService.SentEmails.First();
            sentEmail.Subject.Should().Be($"Portfolio Contact: {contactWithSpecialChars.Subject}");
            sentEmail.Body.Should().Contain(contactWithSpecialChars.Message);
            // In HTML email, special chars should be handled properly
        }

        [Fact]
        public async Task EmailService_RecipientValidation_HandlesVariousEmailFormats()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var testEmails = new[]
            {
                "simple@example.com",
                "user.name+tag@example.com",
                "test.email@subdomain.example.com",
                "user@localhost" // For testing purposes
            };

            // Act
            foreach (var email in testEmails)
            {
                await emailService.SendEmailAsync(email, "Test", "Test body");
            }

            // Assert
            emailService.SentEmails.Should().HaveCount(testEmails.Length);
            emailService.SentEmails.Select(e => e.To).Should().BeEquivalentTo(testEmails);
        }
    }
}