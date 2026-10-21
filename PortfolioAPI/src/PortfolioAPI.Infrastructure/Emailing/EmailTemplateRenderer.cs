using System.Reflection;
using System.Text;

namespace PortfolioAPI.Infrastructure.Emailing
{
    /// <summary>
    /// Loads an embedded HTML email template and substitutes its {{tokens}}.
    ///
    /// Templates live as one file per email type under
    /// <c>Emailing/Templates</c> and are compiled into the assembly as
    /// embedded resources (csproj), so deployments never ship template files
    /// and a template can never go missing at runtime.
    ///
    /// SECURITY CONTRACT: the renderer is deliberately mechanical — it does
    /// NOT encode. Callers must HTML-encode every user-supplied value BEFORE
    /// building the value dictionary (EmailService owns that guarantee so it
    /// stays visible next to the caller-specific values).
    /// </summary>
    public static class EmailTemplateRenderer
    {
        private const string ResourcePrefix = "PortfolioAPI.Infrastructure.Emailing.Templates.";

        public static string Render(string templateName, IReadOnlyDictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("Template name cannot be null or empty.", nameof(templateName));

            var assembly = typeof(EmailTemplateRenderer).Assembly;
            var resourceName = $"{ResourcePrefix}{templateName}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Email template '{templateName}' is not embedded in the Infrastructure assembly " +
                    $"(expected resource '{resourceName}').");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var template = reader.ReadToEnd();

            foreach (var (token, value) in values)
            {
                template = template.Replace("{{" + token + "}}", value, StringComparison.Ordinal);
            }

            return template;
        }
    }
}
