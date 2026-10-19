using System;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a testimonial (api/Testimonials, api/public/testimonials).</summary>
    public class TestimonialDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        /// <summary>1-5 stars.</summary>
        public int Rating { get; set; }
        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public string? ProjectUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
