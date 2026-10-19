using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Payload accepted when creating an experience entry (POST api/Experience).</summary>
    public class CreateExperienceDto
    {
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? Location { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Payload accepted when updating an experience entry (PUT api/Experience/{id}).
    /// Server-managed fields (id, createdAt, updatedAt) are intentionally
    /// absent — the route id wins and timestamps stay server-owned.
    /// </summary>
    public class UpdateExperienceDto
    {
        public string Company { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new();
        public string? Location { get; set; }
        public bool Current { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
