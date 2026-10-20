using System;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a bilingual skill (api/Skills, api/public/skills).</summary>
    public class SkillDto
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string CategoryEn { get; set; } = string.Empty;
        public string CategoryAr { get; set; } = string.Empty;
        /// <summary>0-100.</summary>
        public int Proficiency { get; set; }
        public string? IconUrl { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
