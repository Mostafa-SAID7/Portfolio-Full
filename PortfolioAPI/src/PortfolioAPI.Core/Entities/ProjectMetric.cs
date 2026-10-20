namespace PortfolioAPI.Core.Entities
{
    /// <summary>A bilingual label/value stat displayed on a project card (e.g. "300% faster queries").</summary>
    public class ProjectMetric
    {
        public string LabelEn { get; set; } = string.Empty;
        public string LabelAr { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
