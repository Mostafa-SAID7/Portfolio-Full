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
                .HasMaxLength(100);

            builder.Property(s => s.ProficiencyLevel)
                .HasDefaultValue(50)
                .IsRequired();

            builder.Property(s => s.IconUrl)
                .HasMaxLength(500);

            builder.Property(s => s.IsVisible)
                .HasDefaultValue(true);

            builder.HasIndex(s => s.Name)
                .IsUnique();

            builder.HasIndex(s => s.Category);

            builder.HasIndex(s => s.IsVisible);
        }
    }
}