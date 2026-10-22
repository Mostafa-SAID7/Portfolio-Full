using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(c => c.Subject)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(c => c.IsRead)
                .HasDefaultValue(false);

            builder.Property(c => c.IpAddress)
                .HasMaxLength(45); // IPv6-safe

            builder.Property(c => c.UserAgent)
                .HasMaxLength(500);

            builder.HasIndex(c => c.Email);
            builder.HasIndex(c => c.IsRead);
        }
    }
}
