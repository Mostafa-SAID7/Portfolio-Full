using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Payload accepted when creating an experience entry (POST api/Experience).</summary>
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
        public List<string> Technologies { get; set; } = new();
        public string? LocationEn { get; set; }
        public string? LocationAr { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Payload accepted when updating an experience entry (PUT api/Experience/{id}).
    /// Same shape as create; server-managed fields (id, createdAt, updatedAt)
    /// are intentionally absent — the route id wins and timestamps stay
    /// server-owned.
    /// </summary>
    public class UpdateExperienceDto : CreateExperienceDto
    {
    }
}
