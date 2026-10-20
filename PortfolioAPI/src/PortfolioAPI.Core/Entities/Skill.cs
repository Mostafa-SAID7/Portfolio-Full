using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    public class Skill : BaseEntity
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string CategoryEn { get; set; } = string.Empty;
        public string CategoryAr { get; set; } = string.Empty;
        public int Proficiency { get; set; } // 1-100
        public string? IconUrl { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
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
            NameEn = other.NameEn;
            NameAr = other.NameAr;
            CategoryEn = other.CategoryEn;
            CategoryAr = other.CategoryAr;
            Proficiency = other.Proficiency;
            IconUrl = other.IconUrl;
            DescriptionEn = other.DescriptionEn;
            DescriptionAr = other.DescriptionAr;
            Order = other.Order;
            IsActive = other.IsActive;
        }
    }
}