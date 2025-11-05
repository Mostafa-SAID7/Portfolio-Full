using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    public class Skill : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Proficiency { get; set; } // 1-100
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<SkillProject> SkillProjects { get; set; } = new List<SkillProject>();
    }
}