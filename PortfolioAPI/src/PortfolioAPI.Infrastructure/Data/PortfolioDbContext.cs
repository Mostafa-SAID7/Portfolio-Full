using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectImage> ProjectImages { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<Experience> Experiences { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<ContactReply> ContactReplies { get; set; } = null!;
        public DbSet<Testimonial> Testimonials { get; set; } = null!;
        public DbSet<Credential> Credentials { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Every entity mapping lives in its own IEntityTypeConfiguration
            // under Data/Configurations (one entity = one configuration file).
            // Automatic discovery keeps this context thin and makes a second,
            // conflicting mapping for the same entity structurally impossible
            // — no hand-inlined fluent blocks to drift or duplicate anymore.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
        }
    }
}
