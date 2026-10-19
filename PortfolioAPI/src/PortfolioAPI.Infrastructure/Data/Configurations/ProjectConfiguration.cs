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

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
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
            builder.Property(p => p.Category).HasMaxLength(100);
            builder.Property(p => p.Client).HasMaxLength(100);
            builder.Property(p => p.Type).HasMaxLength(50);
            builder.Property(p => p.Database).HasMaxLength(100);
            builder.Property(p => p.Status).HasMaxLength(50);
            builder.Property(p => p.Badges)
                .HasMaxLength(200)
                .HasConversion(StringListConverter.CommaSeparatedOrNull, StringListConverter.ListComparerOrNull);
            builder.Property(p => p.Metrics)
                .HasMaxLength(2000)
                .HasConversion(JsonListConverter.MetricsJson, JsonListConverter.MetricsComparer);
            builder.Property(p => p.Gradient).HasMaxLength(200);

            builder.HasIndex(p => p.Title);
            builder.HasIndex(p => p.Featured);
            builder.HasIndex(p => p.IsActive);
            builder.HasIndex(p => p.Category);

            // Gallery images
            builder.HasMany(p => p.Images)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
