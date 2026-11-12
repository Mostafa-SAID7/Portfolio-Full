namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted when creating a skill (POST api/Skills).</summary>
    public class CreateSkillDto
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string CategoryEn { get; set; } = string.Empty;
        public string CategoryAr { get; set; } = string.Empty;
        public int Proficiency { get; set; }
        public string? IconUrl { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
