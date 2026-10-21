using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data.Configurations
{
    /// <summary>Join/lookup entity reached through navigation properties.</summary>
    public class SkillProjectConfiguration : IEntityTypeConfiguration<SkillProject>
    {
        public void Configure(EntityTypeBuilder<SkillProject> builder)
        {
            builder.HasKey(sp => new { sp.SkillId, sp.ProjectId });

            builder.HasOne(sp => sp.Skill)
                .WithMany(s => s.SkillProjects)
                .HasForeignKey(sp => sp.SkillId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sp => sp.Project)
                .WithMany()
                .HasForeignKey(sp => sp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
