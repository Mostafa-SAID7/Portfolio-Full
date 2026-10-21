using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    /// <summary>Project categories (managed from the dashboard).</summary>
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.NameEn).IsRequired().HasMaxLength(100);
            builder.Property(c => c.NameAr).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Order).IsRequired();
            builder.Property(c => c.IsActive).IsRequired();
            builder.HasIndex(c => c.NameEn).IsUnique();
        }
    }
}
