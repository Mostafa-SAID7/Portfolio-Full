using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class SkillConfiguration : IEntityTypeConfiguration<Skill>
    {
        public void Configure(EntityTypeBuilder<Skill> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Proficiency)
                // No DB default: with HasDefaultValue(50), a legitimate
                // Proficiency of 0 matched EF's insert sentinel, got omitted
                // from the INSERT, and the database rewrote it to 50 — the API
                // echoed 0 while 50 was stored. Validators own the 0-100 range.
                .IsRequired();

            builder.Property(s => s.IconUrl)
                .HasMaxLength(500);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.Order)
                .HasDefaultValue(0);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(s => s.Name)
                .IsUnique();

            builder.HasIndex(s => s.Category);

            builder.HasIndex(s => s.IsActive);
        }
    }
}
