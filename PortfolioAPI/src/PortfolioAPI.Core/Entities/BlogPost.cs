using System.Collections.Generic;

namespace PortfolioAPI.Core.Entities
{
    public class BlogPost : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? FeaturedImage { get; set; }
        public int ReadingTime { get; set; } // in minutes
        public bool Published { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        // Navigation properties
        public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    }
}