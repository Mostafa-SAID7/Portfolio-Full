namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Fields to change on PATCH api/Categories/{id}; omitted values are preserved.</summary>
    public class UpdateCategoryPatchDto
    {
        public string? NameEn { get; set; }
        public string? NameAr { get; set; }
        public int? Order { get; set; }
        public bool? IsActive { get; set; }
    }
}
