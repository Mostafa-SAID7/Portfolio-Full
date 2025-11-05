namespace PortfolioAPI.Core.Entities
{
    public class ProjectImage : BaseEntity
    {
        public int ProjectId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public int Order { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;

        // Navigation property
        public Project Project { get; set; } = null!;
    }
}