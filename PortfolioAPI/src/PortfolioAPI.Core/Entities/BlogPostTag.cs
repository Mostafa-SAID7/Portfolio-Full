namespace PortfolioAPI.Core.Entities
{
    public class BlogPostTag
    {
        public int BlogPostId { get; set; }
        public string Tag { get; set; } = string.Empty;

        // Navigation property
        public BlogPost BlogPost { get; set; } = null!;
    }
}