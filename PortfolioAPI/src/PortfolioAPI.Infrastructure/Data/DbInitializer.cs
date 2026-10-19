using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data
{
    /// <summary>
    /// Schema management (issues #5/#15): EF Core migrations are the source of
    /// truth — EnsureCreated is gone. Three safe paths:
    ///   1. Database absent (fresh dev)     → Migrate() creates the full schema.
    ///   2. Database exists, no Projects table → treated as fresh → Migrate().
    ///   3. Database exists with the legacy EnsureCreated-era schema → BASELINE:
    ///      only the __EFMigrationsHistory bookkeeping table is created and the
    ///      current migrations are recorded as applied — NO DDL runs against
    ///      existing tables, no data is touched; Migrate() then applies only
    ///      migrations added after the baseline (future schema changes).
    /// PatchSchemaAsync stays as an idempotent safety net for legacy drift
    /// (Projects columns, Credentials table) and removals (blog tables), and
    /// seeds demo content only when the database is empty — it never
    /// overwrites existing rows.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(PortfolioDbContext context)
        {
            await EnsureSchemaAsync(context);
            await PatchSchemaAsync(context);

            if (await context.Projects.AnyAsync() || await context.Skills.AnyAsync())
            {
                return; // Database already has data.
            }

            await SeedSkills(context);
            await SeedProjects(context);
            await SeedExperiences(context);
            await SeedTestimonials(context);
            await context.SaveChangesAsync();
        }

        private static async Task EnsureSchemaAsync(PortfolioDbContext context)
        {
            // Cases 1 + 2: fresh or empty database — migrations create everything.
            if (!await context.Database.CanConnectAsync() || !await TableExistsAsync(context, "Projects"))
            {
                await context.Database.MigrateAsync();
                return;
            }

            // Case 3: existing legacy database without EF bookkeeping. Create ONLY
            // the history table and record every migration in the assembly as
            // applied (they are — the schema pre-existed, built by EnsureCreated +
            // patches), so Migrate() below executes no DDL against existing tables.
            if (!await TableExistsAsync(context, "__EFMigrationsHistory"))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
                    CREATE TABLE [dbo].[__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY,
                        [ProductVersion] nvarchar(32) NOT NULL);");

                foreach (var migrationId in context.Database.GetMigrations())
                {
                    // ProductVersion is informational bookkeeping only.
                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1})",
                        migrationId, "9.0.0");
                }
            }

            // Applies only genuinely pending (future) migrations; no-op otherwise.
            await context.Database.MigrateAsync();
        }

        /// <summary>
        /// Idempotent DDL for schema added after the first EnsureCreated (legacy
        /// drift safety net) and removals. No-ops on migrated schemas.
        /// </summary>
        private static async Task PatchSchemaAsync(PortfolioDbContext context)
        {
            // Blog was removed from the app entirely (no UI, no API, no entities):
            // drop its leftover tables on legacy databases. Idempotent; BlogPostTag
            // first because of its foreign key to BlogPosts.
            if (await TableExistsAsync(context, "BlogPostTag"))
            {
                await context.Database.ExecuteSqlRawAsync("DROP TABLE [dbo].[BlogPostTag];");
            }

            if (await TableExistsAsync(context, "BlogPosts"))
            {
                await context.Database.ExecuteSqlRawAsync("DROP TABLE [dbo].[BlogPosts];");
            }

            if (!await ColumnExistsAsync(context, "Projects", "Category"))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE dbo.Projects
                    ADD Category nvarchar(100) NULL,
                        Client nvarchar(100) NULL,
                        [Type] nvarchar(50) NULL,
                        [Database] nvarchar(100) NULL,
                        Status nvarchar(50) NULL,
                        Badges nvarchar(200) NULL,
                        Metrics nvarchar(2000) NULL,
                        Gradient nvarchar(200) NULL;");
            }

            if (!await TableExistsAsync(context, "Credentials"))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE dbo.Credentials (
                        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Tab nvarchar(30) NOT NULL,
                        Year nvarchar(10) NOT NULL,
                        PeriodEn nvarchar(100) NOT NULL,
                        PeriodAr nvarchar(100) NOT NULL,
                        TitleEn nvarchar(200) NOT NULL,
                        TitleAr nvarchar(200) NOT NULL,
                        OrgEn nvarchar(150) NOT NULL,
                        OrgAr nvarchar(150) NOT NULL,
                        LocationEn nvarchar(150) NULL,
                        LocationAr nvarchar(150) NULL,
                        SummaryEn nvarchar(1000) NULL,
                        SummaryAr nvarchar(1000) NULL,
                        HighlightsEn nvarchar(4000) NULL,
                        HighlightsAr nvarchar(4000) NULL,
                        Stack nvarchar(500) NULL,
                        CredentialId nvarchar(100) NULL,
                        StatusEn nvarchar(50) NULL,
                        StatusAr nvarchar(50) NULL,
                        Featured bit NOT NULL DEFAULT 0,
                        [Order] int NOT NULL DEFAULT 0,
                        IsActive bit NOT NULL DEFAULT 1,
                        CreatedAt datetime2 NOT NULL,
                        UpdatedAt datetime2 NULL,
                        IsDeleted bit NOT NULL DEFAULT 0,
                        DeletedAt datetime2 NULL
                    );");
            }
        }

        private static async Task<bool> ColumnExistsAsync(PortfolioDbContext context, string table, string column)
        {
            var count = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {table} AND COLUMN_NAME = {column}")
                .SingleAsync();
            return count > 0;
        }

        private static async Task<bool> TableExistsAsync(PortfolioDbContext context, string table)
        {
            var count = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {table}")
                .SingleAsync();
            return count > 0;
        }

        private static Task SeedSkills(PortfolioDbContext context)
        {
            var skills = new[]
            {
                new Skill { Name = "C#", Category = "Programming Languages", Proficiency = 90, IsActive = true, Order = 1 },
                new Skill { Name = ".NET", Category = "Frameworks", Proficiency = 88, IsActive = true, Order = 2 },
                new Skill { Name = "ASP.NET Core", Category = "Frameworks", Proficiency = 85, IsActive = true, Order = 3 },
                new Skill { Name = "Entity Framework", Category = "Data Access", Proficiency = 82, IsActive = true, Order = 4 },
                new Skill { Name = "SQL Server", Category = "Databases", Proficiency = 80, IsActive = true, Order = 5 },
                new Skill { Name = "Angular", Category = "Frontend", Proficiency = 85, IsActive = true, Order = 6 },
                new Skill { Name = "React", Category = "Frontend", Proficiency = 80, IsActive = true, Order = 7 },
                new Skill { Name = "TypeScript", Category = "Programming Languages", Proficiency = 82, IsActive = true, Order = 8 },
                new Skill { Name = "JavaScript", Category = "Programming Languages", Proficiency = 85, IsActive = true, Order = 9 },
                new Skill { Name = "HTML/CSS", Category = "Web Technologies", Proficiency = 90, IsActive = true, Order = 10 },
                new Skill { Name = "Git", Category = "Tools", Proficiency = 85, IsActive = true, Order = 11 },
                new Skill { Name = "Docker", Category = "DevOps", Proficiency = 70, IsActive = true, Order = 12 },
                new Skill { Name = "Azure", Category = "Cloud", Proficiency = 72, IsActive = true, Order = 13 }
            };

            context.Skills.AddRange(skills);
            return Task.CompletedTask;
        }

        private static Task SeedProjects(PortfolioDbContext context)
        {
            var projects = new[]
            {
                new Project
                {
                    Title = "Portfolio Platform",
                    Description = "Full portfolio suite: .NET 9 REST API, Angular admin dashboard and a TanStack Start public site, all driven by one centralized backend.",
                    Technologies = new List<string> { ".NET 9", "Angular", "React", "SQL Server" },
                    GithubUrl = "https://github.com/Mostafa-SAID7/Portfolio-Full",
                    LiveUrl = null,
                    Featured = true,
                    Order = 1,
                    IsActive = true
                },
                new Project
                {
                    Title = "Inventory Management System",
                    Description = "E-Inventory platform for tracking stock levels, suppliers and purchase orders with role-based access and reporting.",
                    Technologies = new List<string> { "ASP.NET Core", "SQL Server", "Angular" },
                    GithubUrl = null,
                    LiveUrl = null,
                    Featured = true,
                    Order = 2,
                    IsActive = true
                },
                new Project
                {
                    Title = "Task Management App",
                    Description = "A collaborative task management application with real-time updates and team collaboration features.",
                    Technologies = new List<string> { "Vue.js", "Firebase", "Tailwind CSS" },
                    GithubUrl = null,
                    LiveUrl = null,
                    Featured = false,
                    Order = 3,
                    IsActive = true
                }
            };

            context.Projects.AddRange(projects);
            return Task.CompletedTask;
        }

        private static Task SeedExperiences(PortfolioDbContext context)
        {
            var experiences = new[]
            {
                new Experience
                {
                    Position = "Software Developer",
                    Company = "Freelance",
                    Location = "Remote",
                    StartDate = new DateTime(2023, 1, 1),
                    Current = true,
                    Description = "Building full-stack web applications: .NET APIs, Angular/React frontends and SQL Server databases for clients.",
                    Technologies = new List<string> { ".NET", "Angular", "React", "SQL Server" },
                    Order = 1,
                    IsActive = true
                },
                new Experience
                {
                    Position = "Junior Software Developer",
                    Company = "Tech Solutions Inc.",
                    Location = "Cairo, EG",
                    StartDate = new DateTime(2021, 6, 1),
                    EndDate = new DateTime(2022, 12, 31),
                    Current = false,
                    Description = "Developed and maintained web applications using .NET technologies and modern JavaScript frameworks.",
                    Technologies = new List<string> { "C#", "ASP.NET", "JavaScript", "Git" },
                    Order = 2,
                    IsActive = true
                }
            };

            context.Experiences.AddRange(experiences);
            return Task.CompletedTask;
        }

        private static Task SeedTestimonials(PortfolioDbContext context)
        {
            var testimonials = new[]
            {
                new Testimonial
                {
                    Name = "John Smith",
                    Position = "CTO",
                    Company = "TechCorp",
                    Content = "Excellent work on our platform. The solution was delivered on time and exceeded our expectations in terms of performance and scalability.",
                    Rating = 5,
                    Featured = true,
                    IsActive = true,
                    Order = 1
                },
                new Testimonial
                {
                    Name = "Sarah Johnson",
                    Position = "Project Manager",
                    Company = "InnovateNow",
                    Content = "Professional, reliable, and highly skilled developer. Great communication throughout the project and delivered quality code.",
                    Rating = 5,
                    Featured = false,
                    IsActive = true,
                    Order = 2
                }
            };

            context.Testimonials.AddRange(testimonials);
            return Task.CompletedTask;
        }
    }
}
