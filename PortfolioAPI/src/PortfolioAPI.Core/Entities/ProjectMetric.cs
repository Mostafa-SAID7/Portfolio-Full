namespace PortfolioAPI.Core.Entities
{
    /// <summary>A label/value stat displayed on a project card (e.g. "300% faster queries").</summary>
    public class ProjectMetric
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
