namespace PortfolioAPI.Core.Entities
{
    /// <summary>
    /// Explicit join entity supporting many-to-many relationship between Projects and Websites.
    /// Allows a single canonical project to appear across multiple independent frontend websites,
    /// with website-specific publication visibility and display ordering.
    /// </summary>
    public class ProjectWebsite : BaseEntity
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int WebsiteId { get; set; }
        public Website Website { get; set; } = null!;

        /// <summary>
        /// Whether the project is published specifically on this frontend website.
        /// Defaults to true.
        /// </summary>
        public bool IsPublished { get; set; } = true;

        /// <summary>
        /// Independent display ordering on this specific frontend website.
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
    }
}

