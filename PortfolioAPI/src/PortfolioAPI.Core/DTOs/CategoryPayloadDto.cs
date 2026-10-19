namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a project category (api/Categories, api/public/categories).</summary>
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }

        /// <summary>How many projects use this category (admin list only).</summary>
        public int ProjectCount { get; set; }
    }

    /// <summary>Payload accepted when creating a category (POST api/Categories).</summary>
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Payload accepted when updating a category (PUT api/Categories/{id}).
    /// Server-managed fields (id, createdAt, updatedAt) are intentionally
    /// absent — the route id wins and timestamps stay server-owned.
    /// </summary>
    public class UpdateCategoryDto : CreateCategoryDto
    {
    }
}
