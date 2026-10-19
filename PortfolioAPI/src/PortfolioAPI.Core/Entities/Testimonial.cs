namespace PortfolioAPI.Core.Entities
{
    public class Testimonial : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public bool Featured { get; set; } = false;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? ProjectUrl { get; set; }

        /// <summary>
        /// Copies the editable fields from another instance onto this tracked
        /// one. Attaching the incoming instance directly (the old behaviour)
        /// threw "another instance with the same key ... is already being
        /// tracked" → HTTP 500 on every testimonial update.
        /// </summary>
        public void Apply(Testimonial other)
        {
            Name = other.Name;
            Position = other.Position;
            Company = other.Company;
            Content = other.Content;
            ImageUrl = other.ImageUrl;
            Rating = other.Rating;
            Featured = other.Featured;
            Order = other.Order;
            IsActive = other.IsActive;
            ProjectUrl = other.ProjectUrl;
        }
    }
}