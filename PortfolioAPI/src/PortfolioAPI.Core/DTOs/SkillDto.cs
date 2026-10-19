using System;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a skill (api/Skills, api/public/skills).</summary>
    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        /// <summary>0-100.</summary>
        public int Proficiency { get; set; }
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
