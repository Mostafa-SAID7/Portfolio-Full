namespace PortfolioAPI.Application.DTOs
{
    /// <summary>One localized key-value metric attached to a project.</summary>
    public class ProjectMetricDto
    {
        public string LabelEn { get; set; } = string.Empty;
        public string LabelAr { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
