using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TitleEn)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.TitleAr)
                .IsRequired()
                .HasMaxLength(100);

            // Stable public URL identifier - unique, and never changed by a rename.
            // The filter keeps rows without a slug (written by an older API
            // build) from colliding with each other.
            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(100);
            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Projects_Slug")
                .HasFilter("[Slug] <> ''");

            builder.Property(p => p.DescriptionEn)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(p => p.DescriptionAr)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            builder.Property(p => p.Technologies)
                .HasMaxLength(500)
                .HasConversion(StringListConverter.CommaSeparated, StringListConverter.ListComparer);

            builder.Property(p => p.GithubUrl)
                .HasMaxLength(500);

            builder.Property(p => p.LiveUrl)
                .HasMaxLength(500);

            builder.Property(p => p.Featured)
                .HasDefaultValue(false);

            builder.Property(p => p.Order)
                .HasDefaultValue(0);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            // Editorial fields consumed by the public portfolio.
            builder.Property(p => p.CategoryEn).HasMaxLength(100);
            builder.Property(p => p.CategoryAr).HasMaxLength(100);
            builder.Property(p => p.ClientEn).HasMaxLength(100);
            builder.Property(p => p.ClientAr).HasMaxLength(100);
            builder.Property(p => p.TypeEn).HasMaxLength(50);
            builder.Property(p => p.TypeAr).HasMaxLength(50);
            builder.Property(p => p.DatabaseEn).HasMaxLength(100);
            builder.Property(p => p.DatabaseAr).HasMaxLength(100);
            builder.Property(p => p.StatusEn).HasMaxLength(50);
            builder.Property(p => p.StatusAr).HasMaxLength(50);
            builder.Property(p => p.BadgesEn)
                .HasMaxLength(200)
                .HasConversion(StringListConverter.CommaSeparatedOrNull, StringListConverter.ListComparerOrNull);
            builder.Property(p => p.BadgesAr)
                .HasMaxLength(200)
                .HasConversion(StringListConverter.CommaSeparatedOrNull, StringListConverter.ListComparerOrNull);
            builder.Property(p => p.MetricsEn)
                .HasMaxLength(2000)
                .HasConversion(JsonListConverter.MetricsJson, JsonListConverter.MetricsComparer);
            builder.Property(p => p.MetricsAr)
                .HasMaxLength(2000)
                .HasConversion(JsonListConverter.MetricsJson, JsonListConverter.MetricsComparer);
            builder.Property(p => p.Gradient).HasMaxLength(200);

            builder.HasIndex(p => p.TitleEn);
            builder.HasIndex(p => p.Featured);
            builder.HasIndex(p => p.IsActive);
            builder.HasIndex(p => p.CategoryEn);

            // Gallery images
            builder.HasMany(p => p.Images)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
