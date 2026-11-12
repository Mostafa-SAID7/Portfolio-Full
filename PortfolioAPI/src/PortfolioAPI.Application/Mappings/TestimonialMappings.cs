using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Application.Mappings
{
    /// <summary>Testimonial entity <-> DTO mappings.</summary>
    public static partial class EntityMappings
    {
        public static TestimonialDto ToDto(Testimonial t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Position = t.Position,
            Company = t.Company,
            Content = t.Content,
            ImageUrl = t.ImageUrl,
            Rating = t.Rating,
            Featured = t.Featured,
            Order = t.Order,
            IsActive = t.IsActive,
            ProjectUrl = t.ProjectUrl,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        public static Testimonial ToEntity(CreateTestimonialDto dto, int id = 0) => new()
        {
            Id = id,
            Name = dto.Name?.Trim() ?? string.Empty,
            Position = dto.Position?.Trim() ?? string.Empty,
            Company = dto.Company?.Trim() ?? string.Empty,
            Content = dto.Content?.Trim() ?? string.Empty,
            ImageUrl = dto.ImageUrl,
            Rating = dto.Rating,
            Featured = dto.Featured,
            Order = dto.Order,
            IsActive = dto.IsActive
        };
    }
}
