using System;
using System.Collections.Generic;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted when creating a work experience entry (POST api/Experience).</summary>
    public class CreateExperienceDto
    {
        public string CompanyEn { get; set; } = string.Empty;
        public string CompanyAr { get; set; } = string.Empty;
        public string PositionEn { get; set; } = string.Empty;
        public string PositionAr { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public List<string>? Technologies { get; set; }
        public string? LocationEn { get; set; }
        public string? LocationAr { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
