using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data.Configurations;

namespace PortfolioAPI.Infrastructure.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<Experience> Experiences { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Testimonial> Testimonials { get; set; } = null!;
        public DbSet<BlogPost> BlogPosts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new SkillConfiguration());

            // Configure other entities if needed
            ConfigureExperience(modelBuilder);
            ConfigureContact(modelBuilder);
            ConfigureTestimonial(modelBuilder);
            ConfigureBlogPost(modelBuilder);
        }

        private void ConfigureExperience(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Experience>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.JobTitle)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Location)
                    .HasMaxLength(100);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.IsCurrentPosition)
                    .HasDefaultValue(false);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Technologies)
                    .HasMaxLength(200);

                entity.Property(e => e.CompanyWebsite)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.CompanyName);
                entity.HasIndex(e => e.IsCurrentPosition);
            });
        }

        private void ConfigureContact(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(c => c.Phone)
                    .HasMaxLength(15);

                entity.Property(c => c.Subject)
                    .HasMaxLength(200);

                entity.Property(c => c.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(c => c.IsRead)
                    .HasDefaultValue(false);

                entity.HasIndex(c => c.Email);
                entity.HasIndex(c => c.IsRead);
            });
        }

        private void ConfigureTestimonial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Testimonial>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.ClientName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(t => t.ClientPosition)
                    .HasMaxLength(100);

                entity.Property(t => t.ClientCompany)
                    .HasMaxLength(100);

                entity.Property(t => t.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(t => t.Rating)
                    .HasDefaultValue(5);

                entity.Property(t => t.ClientImageUrl)
                    .HasMaxLength(500);

                entity.Property(t => t.IsVisible)
                    .HasDefaultValue(true);

                entity.HasIndex(t => t.IsVisible);
                entity.HasIndex(t => t.Rating);
            });
        }

        private void ConfigureBlogPost(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(b => b.Slug)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(b => b.Content)
                    .IsRequired();

                entity.Property(b => b.Excerpt)
                    .HasMaxLength(500);

                entity.Property(b => b.Author)
                    .HasMaxLength(100);

                entity.Property(b => b.FeaturedImageUrl)
                    .HasMaxLength(500);

                entity.Property(b => b.Tags)
                    .HasMaxLength(200);

                entity.Property(b => b.IsPublished)
                    .HasDefaultValue(false);

                entity.Property(b => b.ReadTimeMinutes)
                    .HasDefaultValue(0);

                entity.Property(b => b.ViewCount)
                    .HasDefaultValue(0);

                entity.HasIndex(b => b.Slug)
                    .IsUnique();

                entity.HasIndex(b => b.IsPublished);
                entity.HasIndex(b => new { b.IsPublished, b.PublishedAt });
            });
        }
    }
}