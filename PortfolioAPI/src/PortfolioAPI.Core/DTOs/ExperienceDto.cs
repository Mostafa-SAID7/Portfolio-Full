namespace PortfolioAPI.Core.DTOs
{
    public class ExperienceDto
    {
        public int Id { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentPosition { get; set; }
        public string? Description { get; set; }
        public string? Technologies { get; set; }
        public string? CompanyWebsite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}