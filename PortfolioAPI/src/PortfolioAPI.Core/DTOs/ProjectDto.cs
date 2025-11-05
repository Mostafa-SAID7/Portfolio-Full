namespace PortfolioAPI.Core.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Technologies { get; set; }
        public string? ProjectUrl { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}