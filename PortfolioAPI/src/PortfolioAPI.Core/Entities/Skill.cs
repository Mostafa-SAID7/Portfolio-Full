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

        /// <summary>
        /// Copies the editable fields from another (request-shaped) instance
        /// onto this tracked one. Server-owned fields (Id, CreatedAt,
        /// UpdatedAt) are deliberately untouched — attaching a second instance
        /// with the same key is what used to 500 updates.
        /// </summary>
        public void Apply(Skill other)
        {
            Name = other.Name;
            Category = other.Category;
            Proficiency = other.Proficiency;
            IconUrl = other.IconUrl;
            Description = other.Description;
            Order = other.Order;
            IsActive = other.IsActive;
        }
    }
}