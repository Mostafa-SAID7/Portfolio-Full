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

            builder.Property(p => p.Technologies)
                .HasMaxLength(200);

            builder.Property(p => p.ProjectUrl)
                .HasMaxLength(500);

            builder.Property(p => p.RepositoryUrl)
                .HasMaxLength(500);

            builder.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            builder.Property(p => p.IsFeatured)
                .HasDefaultValue(false);

            builder.HasIndex(p => p.Title);

            builder.HasIndex(p => p.IsFeatured);
        }
    }
}