using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class ProjectWebsiteConfiguration : IEntityTypeConfiguration<ProjectWebsite>
    {
        public void Configure(EntityTypeBuilder<ProjectWebsite> builder)
        {
            builder.HasKey(pw => pw.Id);

            builder.Property(pw => pw.IsPublished)
                .HasDefaultValue(true);

            builder.Property(pw => pw.DisplayOrder)
                .HasDefaultValue(0);

            // Prevent duplicate assignments of a project to the same website
            builder.HasIndex(pw => new { pw.ProjectId, pw.WebsiteId })
                .IsUnique()
                .HasDatabaseName("IX_ProjectWebsites_Project_Website");

            // Efficient filtering for website-scoped project listings
            builder.HasIndex(pw => new { pw.WebsiteId, pw.IsPublished, pw.DisplayOrder })
                .HasDatabaseName("IX_ProjectWebsites_Website_Published_Order");

            builder.HasOne(pw => pw.Project)
                .WithMany(p => p.ProjectWebsites)
                .HasForeignKey(pw => pw.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pw => pw.Website)
                .WithMany(w => w.ProjectWebsites)
                .HasForeignKey(pw => pw.WebsiteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

