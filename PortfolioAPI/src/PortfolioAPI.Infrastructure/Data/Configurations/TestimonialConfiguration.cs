using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
    {
        public void Configure(EntityTypeBuilder<Testimonial> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Position)
                .HasMaxLength(100);

            builder.Property(t => t.Company)
                .HasMaxLength(100);

            builder.Property(t => t.Content)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(t => t.ImageUrl)
                .HasMaxLength(500);

            builder.Property(t => t.Rating)
                // No DB default: the model default would act as an insert
                // sentinel for the CLR default (0) and silently rewrite a
                // legitimate 0 to 5. Validators own the 1-5 range.
                .IsRequired();

            builder.Property(t => t.Featured)
                .HasDefaultValue(false);

            builder.Property(t => t.Order)
                .HasDefaultValue(0);

            builder.Property(t => t.IsActive)
                .HasDefaultValue(true);

            builder.Property(t => t.ProjectUrl)
                .HasMaxLength(500);

            builder.HasIndex(t => t.IsActive);
            builder.HasIndex(t => t.Rating);
        }
    }
}
