using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class WebsiteConfiguration : IEntityTypeConfiguration<Website>
    {
        public void Configure(EntityTypeBuilder<Website> builder)
        {
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(w => w.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(w => w.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Websites_Slug");

            builder.Property(w => w.Description)
                .HasMaxLength(500);

            builder.Property(w => w.BaseUrl)
                .HasMaxLength(200);

            builder.Property(w => w.Order)
                .HasDefaultValue(0);

            builder.Property(w => w.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(w => w.IsActive);
            builder.HasIndex(w => w.Order);
        }
    }
}

