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
    }
}