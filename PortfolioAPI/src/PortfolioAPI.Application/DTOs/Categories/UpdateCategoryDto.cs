namespace PortfolioAPI.Application.DTOs
{
    /// <summary>
    /// Payload accepted when updating a category (PUT api/Categories/{id}).
    /// Server-managed fields (id, createdAt, updatedAt) are intentionally
    /// absent – the route id wins and timestamps stay server-owned.
    /// </summary>
    public class UpdateCategoryDto : CreateCategoryDto
    {
    }
}
