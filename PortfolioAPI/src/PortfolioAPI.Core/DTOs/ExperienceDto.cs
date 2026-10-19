using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a work-experience entry (api/Experience, api/public/experience).</summary>
    public class ExperienceDto
    {
        public int Id { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? Location { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
