namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Read model for a project category (api/Categories, api/public/categories).</summary>
    public class CategoryDto
    {
        public int Id { get; set; }
        /// <summary>Display name (English) - the canonical name.</summary>
        public string NameEn { get; set; } = string.Empty;
        /// <summary>Display name (Arabic).</summary>
        public string NameAr { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }

        /// <summary>How many projects use this category (admin list only).</summary>
        public int ProjectCount { get; set; }
    }
}
