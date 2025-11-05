namespace PortfolioAPI.Core.DTOs
{
    public class TestimonialDto
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? ClientPosition { get; set; }
        public string? ClientCompany { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? ClientImageUrl { get; set; }
        public bool IsVisible { get; set; }
        public DateTime? GivenDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}