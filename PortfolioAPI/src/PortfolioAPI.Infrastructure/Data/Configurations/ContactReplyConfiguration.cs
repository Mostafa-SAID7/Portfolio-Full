using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Sent replies, one row per delivered answer. The FK cascade matches the
    /// hard-delete path; a soft-deleted message keeps its reply history.
    /// </summary>
    public class ContactReplyConfiguration : IEntityTypeConfiguration<ContactReply>
    {
        public void Configure(EntityTypeBuilder<ContactReply> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.RecipientEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(r => r.Subject)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(r => r.Body)
                .IsRequired()
                .HasMaxLength(ContactReply.BodyMaxLength);

            builder.Property(r => r.SentAt)
                .IsRequired();

            builder.HasOne(r => r.Contact)
                .WithMany()
                .HasForeignKey(r => r.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.ContactId);
            builder.HasIndex(r => r.SentAt);
        }
    }
}
