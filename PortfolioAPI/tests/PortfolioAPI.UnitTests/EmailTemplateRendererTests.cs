using System.Net;
using System.Reflection;
using FluentAssertions;
using PortfolioAPI.Infrastructure.Emailing;

namespace PortfolioAPI.UnitTests
{
    /// <summary>
    /// Email HTML/Marketing templates travel as embedded resources; these tests
    /// lock the renderer contract and guarantee every shipped template is
    /// actually embedded (a broken csproj wildcard must fail HERE, not on the
    /// first real email).
    /// </summary>
    public class EmailTemplateRendererTests
    {
        [Fact]
        public void EveryShippedTemplateIsEmbeddedInTheAssembly()
        {
            var manifest = typeof(EmailTemplateRenderer).Assembly
                .GetManifestResourceNames();

            foreach (var templateName in new[]
                     {
                         "ContactFormSubmission.html",
                         "Welcome.html",
                         "Notification.html"
                     })
            {
                manifest
                    .Should().Contain(
                        $"PortfolioAPI.Infrastructure.Emailing.Templates.{templateName}",
                        because: $"the '{templateName}' email must survive deployment");
            }
        }

        [Fact]
        public void Render_SubstitutesAllTokens()
        {
            var html = EmailTemplateRenderer.Render(
                "Welcome.html",
                new Dictionary<string, string> { ["UserName"] = "Mostafa" });

            html.Should().Contain("Welcome, Mostafa!");
            html.Should().NotContain("{{UserName}}");
        }

        [Fact]
        public void Render_KeepsUtf8ArabicContent_Intact()
        {
            var html = EmailTemplateRenderer.Render(
                "Notification.html",
                new Dictionary<string, string>
                {
                    ["Title"] = "رسالة جديدة",
                    ["Content"] = "محتوى الرسالة"
                });

            html.Should().Contain("رسالة جديدة");
            html.Should().Contain("محتوى الرسالة");
        }

        [Fact]
        public void Render_MissingTemplateFailsWithAClearMessage()
        {
            var act = () => EmailTemplateRenderer.Render(
                "DoesNotExist.html",
                new Dictionary<string, string>());

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not embedded*");
        }

        [Fact]
        public void ContactFormTemplate_ContainsAllDocumentedSlots()
        {
            var template = ReadEmbeddedTemplate("ContactFormSubmission.html");

            template.Should().Contain("{{FromName}}");
            template.Should().Contain("{{FromEmail}}");
            template.Should().Contain("{{Subject}}");
            template.Should().Contain("{{MessageHtml}}");
        }

        private static string ReadEmbeddedTemplate(string templateName)
        {
            using var stream = typeof(EmailTemplateRenderer).Assembly
                .GetManifestResourceStream($"PortfolioAPI.Infrastructure.Emailing.Templates.{templateName}")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
