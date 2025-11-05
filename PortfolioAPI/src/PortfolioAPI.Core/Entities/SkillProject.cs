namespace PortfolioAPI.Core.Entities
{
    public class SkillProject
    {
        public int SkillId { get; set; }
        public int ProjectId { get; set; }

        // Navigation properties
        public Skill Skill { get; set; } = null!;
        public Project Project { get; set; } = null!;
    }
}