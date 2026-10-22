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

            builder.Property(s => s.NameEn)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.NameAr)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.CategoryEn)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.CategoryAr)
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

            builder.Property(s => s.DescriptionEn)
                .HasMaxLength(500);

            builder.Property(s => s.DescriptionAr)
                .HasMaxLength(500);

            builder.Property(s => s.Order)
                .HasDefaultValue(0);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.HasIndex(s => s.NameEn)
                .IsUnique();

            builder.HasIndex(s => s.CategoryEn);

            builder.HasIndex(s => s.IsActive);

            // Defense-in-depth: the FluentValidation validator owns the range
            // at the application boundary; this keeps direct SQL writes
            // (migrations, manual admin fixes) from storing out-of-range
            // proficiency the UI would render as nonsense.
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Skills_Proficiency", "[Proficiency] BETWEEN 0 AND 100"));
        }
    }
}
