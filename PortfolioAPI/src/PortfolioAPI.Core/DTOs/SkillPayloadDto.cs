namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Payload accepted when creating a skill (POST api/Skills).</summary>
    public class CreateSkillDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        /// <summary>0-100.</summary>
        public int Proficiency { get; set; }

        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Payload accepted when updating a skill (PUT api/Skills/{id}). Same shape
    /// as create (mirroring how UpdateProjectDto relates to CreateProjectDto);
    /// server-managed fields (id, createdAt, updatedAt) are intentionally
    /// absent — the route id wins and timestamps stay server-owned, so clients
    /// can no longer over-post them onto the raw EF entity.
    /// </summary>
    public class UpdateSkillDto : CreateSkillDto
    {
    }
}
