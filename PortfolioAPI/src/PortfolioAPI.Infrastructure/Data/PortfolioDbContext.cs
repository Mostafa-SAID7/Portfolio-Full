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
        public DbSet<ProjectImage> ProjectImages { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<Experience> Experiences { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Testimonial> Testimonials { get; set; } = null!;
        public DbSet<Credential> Credentials { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new SkillConfiguration());

            ConfigureProjectImage(modelBuilder);
            ConfigureExperience(modelBuilder);
            ConfigureContact(modelBuilder);
            ConfigureTestimonial(modelBuilder);
            ConfigureCredential(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureJoinEntities(modelBuilder);
        }

        /// <summary>Project categories (managed from the dashboard).</summary>
        private static void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Order).IsRequired();
                entity.Property(c => c.IsActive).IsRequired();
                entity.HasIndex(c => c.Name).IsUnique();
            });
        }

        private static void ConfigureCredential(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Credential>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Tab).IsRequired().HasMaxLength(30);
                entity.Property(c => c.Year).HasMaxLength(10);
                entity.Property(c => c.PeriodEn).IsRequired().HasMaxLength(100);
                entity.Property(c => c.PeriodAr).IsRequired().HasMaxLength(100);
                entity.Property(c => c.TitleEn).IsRequired().HasMaxLength(200);
                entity.Property(c => c.TitleAr).IsRequired().HasMaxLength(200);
                entity.Property(c => c.OrgEn).IsRequired().HasMaxLength(150);
                entity.Property(c => c.OrgAr).IsRequired().HasMaxLength(150);
                entity.Property(c => c.LocationEn).HasMaxLength(150);
                entity.Property(c => c.LocationAr).HasMaxLength(150);
                entity.Property(c => c.SummaryEn).HasMaxLength(1000);
                entity.Property(c => c.SummaryAr).HasMaxLength(1000);
                entity.Property(c => c.HighlightsEn)
                    .HasMaxLength(4000)
                    .HasConversion(JsonListConverter.HighlightsJson, StringListConverter.ListComparerOrNull);
                entity.Property(c => c.HighlightsAr)
                    .HasMaxLength(4000)
                    .HasConversion(JsonListConverter.HighlightsJson, StringListConverter.ListComparerOrNull);
                entity.Property(c => c.Stack)
                    .HasMaxLength(500)
                    .HasConversion(StringListConverter.CommaSeparatedOrNull, StringListConverter.ListComparerOrNull);
                entity.Property(c => c.CredentialId).HasMaxLength(100);
                entity.Property(c => c.StatusEn).HasMaxLength(50);
                entity.Property(c => c.StatusAr).HasMaxLength(50);
                entity.Property(c => c.Featured).HasDefaultValue(false);
                entity.Property(c => c.Order).HasDefaultValue(0);
                entity.Property(c => c.IsActive).HasDefaultValue(true);

                entity.HasIndex(c => c.Tab);
                entity.HasIndex(c => c.IsActive);
            });
        }

        /// <summary>Join/lookup entities reached through navigation properties.</summary>
        private static void ConfigureJoinEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SkillProject>(entity =>
            {
                entity.HasKey(sp => new { sp.SkillId, sp.ProjectId });

                entity.HasOne(sp => sp.Skill)
                    .WithMany(s => s.SkillProjects)
                    .HasForeignKey(sp => sp.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sp => sp.Project)
                    .WithMany()
                    .HasForeignKey(sp => sp.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureProjectImage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectImage>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(i => i.AltText)
                    .HasMaxLength(200);

                entity.Property(i => i.Order)
                    .HasDefaultValue(0);

                entity.Property(i => i.IsPrimary)
                    .HasDefaultValue(false);
            });
        }

        private static void ConfigureExperience(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Experience>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Company)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Position)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Technologies)
                    .HasMaxLength(500)
                    .HasConversion(StringListConverter.CommaSeparated, StringListConverter.ListComparer);

                entity.Property(e => e.Location)
                    .HasMaxLength(100);

                entity.Property(e => e.Current)
                    .HasDefaultValue(false);

                entity.Property(e => e.Order)
                    .HasDefaultValue(0);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.Company);
                entity.HasIndex(e => e.Current);
            });
        }

        private static void ConfigureContact(ModelBuilder modelBuilder)
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

                entity.Property(c => c.Subject)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(c => c.IsRead)
                    .HasDefaultValue(false);

                entity.Property(c => c.IpAddress)
                    .HasMaxLength(45); // IPv6-safe

                entity.Property(c => c.UserAgent)
                    .HasMaxLength(500);

                entity.HasIndex(c => c.Email);
                entity.HasIndex(c => c.IsRead);
            });
        }

        private static void ConfigureTestimonial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Testimonial>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(t => t.Position)
                    .HasMaxLength(100);

                entity.Property(t => t.Company)
                    .HasMaxLength(100);

                entity.Property(t => t.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(t => t.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(t => t.Rating)
                    // No DB default: the model default would act as an insert
                    // sentinel for the CLR default (0) and silently rewrite a
                    // legitimate 0 to 5. Validators own the 1-5 range.
                    .IsRequired();

                entity.Property(t => t.Featured)
                    .HasDefaultValue(false);

                entity.Property(t => t.Order)
                    .HasDefaultValue(0);

                entity.Property(t => t.IsActive)
                    .HasDefaultValue(true);

                entity.Property(t => t.ProjectUrl)
                    .HasMaxLength(500);

                entity.HasIndex(t => t.IsActive);
                entity.HasIndex(t => t.Rating);
            });
        }
    }
}
