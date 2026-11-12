using System;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Read model for a website (api/Websites, api/public/websites).</summary>
    public class WebsiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BaseUrl { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public int AssignedProjectsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
