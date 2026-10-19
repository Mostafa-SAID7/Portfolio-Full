using System;
using System.Collections.Generic;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a blog post (api/Blog, api/public/blog).</summary>
    public class BlogPostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? FeaturedImage { get; set; }
        /// <summary>Minutes.</summary>
        public int ReadingTime { get; set; }
        public bool Published { get; set; }
        public int ViewCount { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
