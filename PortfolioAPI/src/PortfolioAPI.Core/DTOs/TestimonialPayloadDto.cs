namespace PortfolioAPI.Core.DTOs
{
    /// <summary>
    /// Payload accepted when creating a testimonial (POST api/Testimonials).
    /// Binding DTOs (not the raw EF entity) so server-managed fields
    /// (id, createdAt, updatedAt, isDeleted) can never be over-posted.
    /// </summary>
    public class CreateTestimonialDto
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        /// <summary>1-5 stars.</summary>
        public int Rating { get; set; }

        public bool Featured { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProjectUrl { get; set; }
    }

    /// <summary>Payload accepted when updating a testimonial (PUT api/Testimonials/{id}).</summary>
    public class UpdateTestimonialDto : CreateTestimonialDto
    {
    }
}
