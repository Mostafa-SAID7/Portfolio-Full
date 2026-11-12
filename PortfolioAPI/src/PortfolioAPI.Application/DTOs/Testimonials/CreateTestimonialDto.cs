namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted when creating a testimonial (POST api/Testimonials).</summary>
    public class CreateTestimonialDto
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Rating { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProjectUrl { get; set; }
    }
}
