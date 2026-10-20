using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a bilingual work-experience entry (api/Experience, api/public/experience).</summary>
    public class ExperienceDto
    {
        public int Id { get; set; }
        public string CompanyEn { get; set; } = string.Empty;
        public string CompanyAr { get; set; } = string.Empty;
        public string PositionEn { get; set; } = string.Empty;
        public string PositionAr { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? LocationEn { get; set; }
        public string? LocationAr { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
