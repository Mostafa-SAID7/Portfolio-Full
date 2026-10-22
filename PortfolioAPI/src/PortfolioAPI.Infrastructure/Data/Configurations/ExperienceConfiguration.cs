using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
    {
        public void Configure(EntityTypeBuilder<Experience> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.CompanyEn)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.CompanyAr)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.PositionEn)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.PositionAr)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.StartDate)
                .IsRequired();

            builder.Property(e => e.DescriptionEn)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.DescriptionAr)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.Technologies)
                .HasMaxLength(500)
                .HasConversion(StringListConverter.CommaSeparated, StringListConverter.ListComparer);

            builder.Property(e => e.LocationEn)
                .HasMaxLength(100);

            builder.Property(e => e.LocationAr)
                .HasMaxLength(100);

            builder.Property(e => e.Current)
                .HasDefaultValue(false);

            builder.Property(e => e.Order)
                .HasDefaultValue(0);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(e => e.CompanyEn);
            builder.HasIndex(e => e.Current);
        }
    }
}
