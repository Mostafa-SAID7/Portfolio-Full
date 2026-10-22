using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class ProjectImageConfiguration : IEntityTypeConfiguration<ProjectImage>
    {
        public void Configure(EntityTypeBuilder<ProjectImage> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(i => i.AltText)
                .HasMaxLength(200);

            builder.Property(i => i.Order)
                .HasDefaultValue(0);

            builder.Property(i => i.IsPrimary)
                .HasDefaultValue(false);

            // EF convention auto-indexes every FK (this one exists since the
            // InitialCreate migration as IX_ProjectImages_ProjectId); declaring
            // it explicitly pins that guarantee if the model ever changes.
            builder.HasIndex(i => i.ProjectId);
        }
    }
}
