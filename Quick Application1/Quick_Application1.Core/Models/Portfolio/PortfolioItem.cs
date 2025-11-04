// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Quick_Application1.Core.Models.Portfolio
{
    public class PortfolioItem : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? Content { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? ProjectUrl { get; set; }

        [MaxLength(1000)]
        public string? Technologies { get; set; }

        public PortfolioItemType ItemType { get; set; } = PortfolioItemType.Project;

        public SkillLevel? SkillLevel { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [MaxLength(200)]
        public string? Institution { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;
    }
}