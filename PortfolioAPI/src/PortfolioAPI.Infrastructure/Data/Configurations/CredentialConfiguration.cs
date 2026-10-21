using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
    {
        public void Configure(EntityTypeBuilder<Credential> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Tab).IsRequired().HasMaxLength(30);
            builder.Property(c => c.Year).HasMaxLength(10);
            builder.Property(c => c.PeriodEn).IsRequired().HasMaxLength(100);
            builder.Property(c => c.PeriodAr).IsRequired().HasMaxLength(100);
            builder.Property(c => c.TitleEn).IsRequired().HasMaxLength(200);
            builder.Property(c => c.TitleAr).IsRequired().HasMaxLength(200);
            builder.Property(c => c.OrgEn).IsRequired().HasMaxLength(150);
            builder.Property(c => c.OrgAr).IsRequired().HasMaxLength(150);
            builder.Property(c => c.LocationEn).HasMaxLength(150);
            builder.Property(c => c.LocationAr).HasMaxLength(150);
            builder.Property(c => c.SummaryEn).HasMaxLength(1000);
            builder.Property(c => c.SummaryAr).HasMaxLength(1000);
            builder.Property(c => c.HighlightsEn)
                .HasMaxLength(4000)
                .HasConversion(JsonListConverter.HighlightsJson, StringListConverter.ListComparerOrNull);
            builder.Property(c => c.HighlightsAr)
                .HasMaxLength(4000)
                .HasConversion(JsonListConverter.HighlightsJson, StringListConverter.ListComparerOrNull);
            builder.Property(c => c.Stack)
                .HasMaxLength(500)
                .HasConversion(StringListConverter.CommaSeparatedOrNull, StringListConverter.ListComparerOrNull);
            builder.Property(c => c.CredentialId).HasMaxLength(100);
            builder.Property(c => c.StatusEn).HasMaxLength(50);
            builder.Property(c => c.StatusAr).HasMaxLength(50);
            builder.Property(c => c.Featured).HasDefaultValue(false);
            builder.Property(c => c.Order).HasDefaultValue(0);
            builder.Property(c => c.IsActive).HasDefaultValue(true);

            builder.HasIndex(c => c.Tab);
            builder.HasIndex(c => c.IsActive);
        }
    }
}
