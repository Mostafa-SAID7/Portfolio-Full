using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data;

namespace PortfolioAPI.Infrastructure.Data
{
    /// <summary>
    /// Idempotent database initializer. Runs on application startup (Program.cs).
    /// Ensures schema compatibility with existing databases and seeds reference data.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(PortfolioDbContext context)
        {
            // Check if the database has any tables at all (fresh DB vs existing)
            var tables = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
                .SingleAsync();

            if (tables == 0)
            {
                // Fresh database - let EF migrations create everything
                return;
            }

            // Existing database - ensure schema compatibility and seed reference data
            await EnsureSchemaCompatibilityAsync(context);
            await SeedReferenceDataAsync(context);
        }

        private static async Task EnsureSchemaCompatibilityAsync(PortfolioDbContext context)
        {
            // The Category column on Projects was added by migration.
            // If the column is missing (extremely old DB), add it.
            if (!await ColumnExistsAsync(context, "Projects", "Category"))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE dbo.Projects
                    ADD Category nvarchar(100) NULL;
                ");
            }

            // The SkillProject join table was added by migration.
            // If it's missing, create it.
            if (!await TableExistsAsync(context, "SkillProject"))
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE dbo.SkillProject (
                        SkillId int NOT NULL,
                        ProjectId int NOT NULL,
                        CONSTRAINT PK_SkillProject PRIMARY KEY (SkillId, ProjectId),
                        CONSTRAINT FK_SkillProject_Projects_ProjectId FOREIGN KEY (ProjectId)
                            REFERENCES dbo.Projects (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_SkillProject_Skills_SkillId FOREIGN KEY (SkillId)
                            REFERENCES dbo.Skills (Id) ON DELETE CASCADE
                    );
                ");
            }

            // Sync categories from projects - registers every category the projects
            // already use so the dashboard can manage them immediately.
            await SyncCategoriesAsync(context);
        }

        /// <summary>
        /// Registers every category the projects already use as a managed
        /// category, so the dashboard can rename/reorder/hide them right away
        /// (the projects keep their category as plain text). Idempotent.
        /// </summary>
        private static async Task SyncCategoriesAsync(PortfolioDbContext context)
        {
            var existing = await context.Categories.ToListAsync();
            var known = existing.Select(c => c.NameEn).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var used = (await context.Projects.ToListAsync())
                .Select(p => p.CategoryEn)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(c => !known.Contains(c))
                .ToList();

            if (used.Count == 0)
            {
                return;
            }

            var order = existing.Count == 0 ? 0 : existing.Max(c => c.Order) + 1;

            foreach (var name in used)
            {
                context.Categories.Add(new Category
                {
                    NameEn = name,
                    NameAr = string.Empty,
                    IsActive = true,
                    Order = order++
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task<bool> ColumnExistsAsync(PortfolioDbContext context, string table, string column)
        {
            var count = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'")
                .SingleAsync();
            return count > 0;
        }

        private static async Task<bool> TableExistsAsync(PortfolioDbContext context, string table)
        {
            var count = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'")
                .SingleAsync();
            return count > 0;
        }

        private static async Task SeedReferenceDataAsync(PortfolioDbContext context)
        {
            // Only seed if tables are empty
            var anyProjects = await context.Projects.AnyAsync();
            var anySkills = await context.Skills.AnyAsync();
            var anyExperiences = await context.Experiences.AnyAsync();
            var anyCategories = await context.Categories.AnyAsync();
            var anyTestimonials = await context.Testimonials.AnyAsync();
            var anyCredentials = await context.Credentials.AnyAsync();

            if (!anyProjects)
                await SeedProjects(context);

            if (!anySkills)
                await SeedSkills(context);

            if (!anyExperiences)
                await SeedExperiences(context);

            if (!anyCategories)
                await SeedCategories(context);

            if (!anyTestimonials)
                await SeedTestimonials(context);

            if (!anyCredentials)
                await SeedCredentials(context);

            await context.SaveChangesAsync();
        }

        private static Task SeedCategories(PortfolioDbContext context)
        {
            var categories = new[]
            {
                new Category { NameEn = "Clothing", NameAr = "Ù…Ù„Ø§Ø¨Ø³", Order = 1, IsActive = true },
                new Category { NameEn = "Beauty", NameAr = "Ø¬Ù…Ø§Ù„", Order = 2, IsActive = true },
                new Category { NameEn = "Jewelry", NameAr = "Ù…Ø¬ÙˆÙ‡Ø±Ø§Øª", Order = 3, IsActive = true },
                new Category { NameEn = "Furniture", NameAr = "Ø£Ø«Ø§Ø«", Order = 4, IsActive = true },
                new Category { NameEn = "Dashboard", NameAr = "Ù„ÙˆØ­Ø© ØªØ­ÙƒÙ…", Order = 5, IsActive = true },
                new Category { NameEn = "Web3", NameAr = "ÙˆÙŠØ¨ 3", Order = 6, IsActive = true },
                new Category { NameEn = "Sports", NameAr = "Ø±ÙŠØ§Ø¶Ø©", Order = 7, IsActive = true },
                new Category { NameEn = "Tech Accessories", NameAr = "Ø¥ÙƒØ³Ø³ÙˆØ§Ø±Ø§Øª ØªÙ‚Ù†ÙŠØ©", Order = 8, IsActive = true },
                new Category { NameEn = "Backend", NameAr = "Ø¨Ø§Ùƒ Ø¥Ù†Ø¯", Order = 9, IsActive = true },
                new Category { NameEn = "Other", NameAr = "Ø£Ø®Ø±Ù‰", Order = 10, IsActive = true }
            };

            context.Categories.AddRange(categories);
            return Task.CompletedTask;
        }

        context.Categories.AddRange(categories);
            return Task.CompletedTask;
        }

        private static Task SeedSkills(PortfolioDbContext context)
        {
            var skills = new[]
            {
                new Skill { NameEn = "C#", NameAr = "Ø³ÙŠ Ø´Ø§Ø±Ø¨", CategoryEn = "Programming Languages", CategoryAr = "Ù„ØºØ§Øª Ø§Ù„Ø¨Ø±Ù…Ø¬Ø©", Proficiency = 90, IsActive = true, Order = 1 },
                new Skill { NameEn = ".NET", NameAr = "Ø¯ÙˆØª Ù†Øª", CategoryEn = "Frameworks", CategoryAr = "Ø£Ø·Ø± Ø§Ù„Ø¹Ù…Ù„", Proficiency = 88, IsActive = true, Order = 2 },
                new Skill { NameEn = "ASP.NET Core", NameAr = "Ø¥ÙŠÙ‡ Ø¥Ø³ Ø¨ÙŠ Ø¯ÙˆØª Ù†Øª ÙƒÙˆØ±", CategoryEn = "Frameworks", CategoryAr = "Ø£Ø·Ø± Ø§Ù„Ø¹Ù…Ù„", Proficiency = 85, IsActive = true, Order = 3 },
                new Skill { NameEn = "Entity Framework", NameAr = "Ø¥Ù†ØªØªÙŠ ÙØ±ÙŠÙ…ÙˆØ±Ùƒ", CategoryEn = "Data Access", CategoryAr = "Ø§Ù„ÙˆØµÙˆÙ„ Ù„Ù„Ø¨ÙŠØ§Ù†Ø§Øª", Proficiency = 82, IsActive = true, Order = 4 },
                new Skill { NameEn = "SQL Server", NameAr = "Ø¥Ø³ ÙƒÙŠÙˆ Ø¥Ù„ Ø³ÙŠØ±ÙØ±", CategoryEn = "Databases", CategoryAr = "Ù‚ÙˆØ§Ø¹Ø¯ Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª", Proficiency = 80, IsActive = true, Order = 5 },
                new Skill { NameEn = "Angular", NameAr = "Ø£Ù†Ø¬ÙˆÙ„Ø§Ø±", CategoryEn = "Frontend", CategoryAr = "Ø§Ù„ÙˆØ§Ø¬Ù‡Ø© Ø§Ù„Ø£Ù…Ø§Ù…ÙŠØ©", Proficiency = 85, IsActive = true, Order = 6 },
                new Skill { NameEn = "React", NameAr = "Ø±ÙŠØ§ÙƒØª", CategoryEn = "Frontend", CategoryAr = "Ø§Ù„ÙˆØ§Ø¬Ù‡Ø© Ø§Ù„Ø£Ù…Ø§Ù…ÙŠØ©", Proficiency = 80, IsActive = true, Order = 7 },
                new Skill { NameEn = "TypeScript", NameAr = "ØªØ§ÙŠØ¨ Ø³ÙƒØ±ÙŠØ¨Øª", CategoryEn = "Programming Languages", CategoryAr = "Ù„ØºØ§Øª Ø§Ù„Ø¨Ø±Ù…Ø¬Ø©", Proficiency = 82, IsActive = true, Order = 8 },
                new Skill { NameEn = "JavaScript", NameAr = "Ø¬Ø§ÙØ§ Ø³ÙƒØ±ÙŠØ¨Øª", CategoryEn = "Programming Languages", CategoryAr = "Ù„ØºØ§Øª Ø§Ù„Ø¨Ø±Ù…Ø¬Ø©", Proficiency = 85, IsActive = true, Order = 9 },
                new Skill { NameEn = "HTML/CSS", NameAr = "Ø¥ØªØ´ ØªÙŠ Ø¥Ù… Ø¥Ù„ / Ø³ÙŠ Ø¥Ø³ Ø¥Ø³", CategoryEn = "Web Technologies", CategoryAr = "ØªÙ‚Ù†ÙŠØ§Øª Ø§Ù„ÙˆÙŠØ¨", Proficiency = 90, IsActive = true, Order = 10 },
                new Skill { NameEn = "Git", NameAr = "Ø¬ÙŠØª", CategoryEn = "Tools", CategoryAr = "Ø§Ù„Ø£Ø¯ÙˆØ§Øª", Proficiency = 85, IsActive = true, Order = 11 },
                new Skill { NameEn = "Docker", NameAr = "Ø¯ÙˆÙƒÙŠØ±", CategoryEn = "DevOps", CategoryAr = "Ø¯ÙŠÙ Ø£ÙˆØ¨Ø³", Proficiency = 70, IsActive = true, Order = 12 },
                new Skill { NameEn = "Azure", NameAr = "Ø£Ø²ÙˆØ±", CategoryEn = "Cloud", CategoryAr = "Ø§Ù„Ø³Ø­Ø§Ø¨Ø©", Proficiency = 72, IsActive = true, Order = 13 }
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
                    TitleEn = "Portfolio Platform",
                    TitleAr = "Ù…Ù†ØµØ© Ø§Ù„Ù…Ø­ÙØ¸Ø©",
                    DescriptionEn = "Full portfolio suite: .NET 9 REST API, Angular admin dashboard and a TanStack Start public site, all driven by one centralized backend.",
                    DescriptionAr = "Ù…Ø¬Ù…ÙˆØ¹Ø© Ù…Ø­ÙØ¸Ø© ÙƒØ§Ù…Ù„Ø©: .NET 9 REST APIØŒ Ù„ÙˆØ­Ø© ØªØ­ÙƒÙ… Ø£Ù†Ø¬Ù„Ø§Ø± Ø§Ù„Ø¥Ø¯Ø§Ø±ÙŠØ© ÙˆÙ…ÙˆÙ‚Ø¹ Ø¹Ø§Ù… TanStack StartØŒ ÙƒÙ„Ù‡Ø§ Ù…Ø¯Ø¹ÙˆÙ…Ø© Ø¨Ø®Ù„ÙÙŠØ© Ù…Ø±ÙƒØ²ÙŠØ© ÙˆØ§Ø­Ø¯Ø©.",
                    Technologies = new List<string> { ".NET 9", "Angular", "React", "SQL Server" },
                    GithubUrl = "https://github.com/Mostafa-SAID7/Portfolio-Full",
                    LiveUrl = null,
                    Featured = true,
                    Order = 1,
                    IsActive = true
                },
                new Project
                {
                    TitleEn = "Inventory Management System",
                    TitleAr = "Ù†Ø¸Ø§Ù… Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ù…Ø®Ø²ÙˆÙ†",
                    DescriptionEn = "E-Inventory platform for tracking stock levels, suppliers and purchase orders with role-based access and reporting.",
                    DescriptionAr = "Ù…Ù†ØµØ© E-Inventory Ù„ØªØªØ¨Ø¹ Ù…Ø³ØªÙˆÙŠØ§Øª Ø§Ù„Ù…Ø®Ø²ÙˆÙ† ÙˆØ§Ù„Ù…ÙˆØ±Ø¯ÙŠÙ† ÙˆØ£ÙˆØ§Ù…Ø± Ø§Ù„Ø´Ø±Ø§Ø¡ Ù…Ø¹ ÙˆØµÙˆÙ„ ÙŠØ¹ØªÙ…Ø¯ Ø¹Ù„Ù‰ Ø§Ù„Ø£Ø¯ÙˆØ§Ø± ÙˆØ§Ù„ØªÙ‚Ø§Ø±ÙŠØ±.",
                    Technologies = new List<string> { "ASP.NET Core", "SQL Server", "Angular" },
                    GithubUrl = null,
                    LiveUrl = null,
                    Featured = true,
                    Order = 2,
                    IsActive = true
                },
                new Project
                {
                    TitleEn = "Task Management App",
                    TitleAr = "ØªØ·Ø¨ÙŠÙ‚ Ø¥Ø¯Ø§Ø±Ø© Ø§Ù„Ù…Ù‡Ø§Ù…",
                    DescriptionEn = "A collaborative task management application with real-time updates and team collaboration features.",
                    DescriptionAr = "ØªØ·Ø¨ÙŠÙ‚ Ø¥Ø¯Ø§Ø±Ø© Ù…Ù‡Ø§Ù… ØªØ¹Ø§ÙˆÙ†ÙŠ Ù…Ø¹ ØªØ­Ø¯ÙŠØ«Ø§Øª ÙÙŠ Ø§Ù„ÙˆÙ‚Øª Ø§Ù„ÙØ¹Ù„ÙŠ ÙˆÙ…ÙŠØ²Ø§Øª ØªØ¹Ø§ÙˆÙ† Ø§Ù„ÙØ±ÙŠÙ‚.",
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
                    PositionEn = "Software Developer",
                    PositionAr = "Ù…Ø·ÙˆØ± Ø¨Ø±Ù…Ø¬ÙŠØ§Øª",
                    CompanyEn = "Freelance",
                    CompanyAr = "Ø¹Ù…Ù„ Ø­Ø±",
                    LocationEn = "Remote",
                    LocationAr = "Ø¹Ù† Ø¨Ø¹Ø¯",
                    StartDate = new DateTime(2023, 1, 1),
                    Current = true,
                    DescriptionEn = "Building full-stack web applications: .NET APIs, Angular/React frontends and SQL Server databases for clients.",
                    DescriptionAr = "Ø¨Ù†Ø§Ø¡ ØªØ·Ø¨ÙŠÙ‚Ø§Øª ÙˆÙŠØ¨ ÙƒØ§Ù…Ù„Ø©: ÙˆØ§Ø¬Ù‡Ø§Øª .NET APIsØŒ ÙˆÙˆØ§Ø¬Ù‡Ø§Øª Ø£Ù…Ø§Ù…ÙŠØ© Angular/React ÙˆÙ‚ÙˆØ§Ø¹Ø¯ Ø¨ÙŠØ§Ù†Ø§Øª SQL Server Ù„Ù„Ø¹Ù…Ù„Ø§Ø¡.",
                    Technologies = new List<string> { ".NET", "Angular", "React", "SQL Server" },
                    Order = 1,
                    IsActive = true
                },
                new Experience
                {
                    PositionEn = "Junior Software Developer",
                    PositionAr = "Ù…Ø·ÙˆØ± Ø¨Ø±Ù…Ø¬ÙŠØ§Øª Ù…Ø¨ØªØ¯Ø¦",
                    CompanyEn = "Tech Solutions Inc.",
                    CompanyAr = "ØªÙƒ Ø³ÙˆÙ„ÙŠÙˆØ´Ù†Ø²",
                    LocationEn = "Cairo, EG",
                    LocationAr = "Ø§Ù„Ù‚Ø§Ù‡Ø±Ø©ØŒ Ù…ØµØ±",
                    StartDate = new DateTime(2021, 6, 1),
                    EndDate = new DateTime(2022, 12, 31),
                    Current = false,
                    DescriptionEn = "Developed and maintained web applications using .NET technologies and modern JavaScript frameworks.",
                    DescriptionAr = "ØªØ·ÙˆÙŠØ± ÙˆØµÙŠØ§Ù†Ø© ØªØ·Ø¨ÙŠÙ‚Ø§Øª Ø§Ù„ÙˆÙŠØ¨ Ø¨Ø§Ø³ØªØ®Ø¯Ø§Ù… ØªÙ‚Ù†ÙŠØ§Øª .NET ÙˆØ£Ø·Ø± Ø¹Ù…Ù„ JavaScript Ø§Ù„Ø­Ø¯ÙŠØ«Ø©.",
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
                    Order = 1,
                    IsActive = true
                },
                new Testimonial
                {
                    Name = "Sarah Johnson",
                    Position = "Product Manager",
                    Company = "StartupXYZ",
                    Content = "The team delivered a high-quality solution with excellent communication throughout the project. Highly recommended.",
                    Rating = 5,
                    Featured = false,
                    Order = 2,
                    IsActive = true
                }
            };

            context.Testimonials.AddRange(testimonials);
            return Task.CompletedTask;
        }

        private static Task SeedCredentials(PortfolioDbContext context)
        {
            var credentials = new[]
            {
                new Credential
                {
                    Tab = "work",
                    Year = "2023",
                    PeriodEn = "2023 - Present",
                    PeriodAr = "2023 - Ø§Ù„Ø­Ø§Ø¶Ø±",
                    TitleEn = "Software Developer",
                    TitleAr = "Ù…Ø·ÙˆØ± Ø¨Ø±Ù…Ø¬ÙŠØ§Øª",
                    OrgEn = "Freelance",
                    OrgAr = "Ø¹Ù…Ù„ Ø­Ø±",
                    LocationEn = "Remote",
                    LocationAr = "Ø¹Ù† Ø¨Ø¹Ø¯",
                    SummaryEn = "Building full-stack web applications: .NET APIs, Angular/React frontends and SQL Server databases for clients.",
                    SummaryAr = "Ø¨Ù†Ø§Ø¡ ØªØ·Ø¨ÙŠÙ‚Ø§Øª ÙˆÙŠØ¨ ÙƒØ§Ù…Ù„Ø©: ÙˆØ§Ø¬Ù‡Ø§Øª .NET APIsØŒ ÙˆÙˆØ§Ø¬Ù‡Ø§Øª Ø£Ù…Ø§Ù…ÙŠØ© Angular/React ÙˆÙ‚ÙˆØ§Ø¹Ø¯ Ø¨ÙŠØ§Ù†Ø§Øª SQL Server Ù„Ù„Ø¹Ù…Ù„Ø§Ø¡.",
                    HighlightsEn = new List<string> { "Designed and implemented REST APIs", "Optimized SQL queries for 50% performance improvement" },
                    HighlightsAr = new List<string> { "ØªØµÙ…ÙŠÙ… ÙˆØªÙ†ÙÙŠØ° ÙˆØ§Ø¬Ù‡Ø§Øª REST APIs", "ØªØ­Ø³ÙŠÙ† Ø§Ø³ØªØ¹Ù„Ø§Ù…Ø§Øª SQL Ù„ØªØ­Ø³ÙŠÙ† Ø§Ù„Ø£Ø¯Ø§Ø¡ Ø¨Ù†Ø³Ø¨Ø© 50%" },
                    Stack = new List<string> { ".NET", "Angular", "React", "SQL Server" },
                    Order = 1,
                    IsActive = true
                },
                new Credential
                {
                    Tab = "education",
                    Year = "2020",
                    PeriodEn = "2018 - 2020",
                    PeriodAr = "2018 - 2020",
                    TitleEn = "Bachelor of Computer Science",
                    TitleAr = "Ø¨ÙƒØ§Ù„ÙˆØ±ÙŠÙˆØ³ Ø¹Ù„ÙˆÙ… Ø§Ù„Ø­Ø§Ø³Ø¨",
                    OrgEn = "University of Technology",
                    OrgAr = "Ø¬Ø§Ù…Ø¹Ø© Ø§Ù„ØªÙƒÙ†ÙˆÙ„ÙˆØ¬ÙŠØ§",
                    LocationEn = "Cairo, Egypt",
                    LocationAr = "Ø§Ù„Ù‚Ø§Ù‡Ø±Ø©ØŒ Ù…ØµØ±",
                    SummaryEn = "Focused on software engineering, algorithms and database systems.",
                    SummaryAr = "Ø§Ù„ØªØ±ÙƒÙŠØ² Ø¹Ù„Ù‰ Ù‡Ù†Ø¯Ø³Ø© Ø§Ù„Ø¨Ø±Ù…Ø¬ÙŠØ§ØªØŒ Ø§Ù„Ø®ÙˆØ§Ø±Ø²Ù…ÙŠØ§Øª ÙˆØ£Ù†Ø¸Ù…Ø© Ù‚ÙˆØ§Ø¹Ø¯ Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª.",
                    HighlightsEn = new List<string> { "Graduated with honors", "Final project: Distributed Systems" },
                    HighlightsAr = new List<string> { "ØªØ®Ø±Ø¬ Ø¨ØªÙÙˆÙ‚", "Ù…Ø´Ø±ÙˆØ¹ Ø§Ù„ØªØ®Ø±Ø¬: Ø§Ù„Ø£Ù†Ø¸Ù…Ø© Ø§Ù„Ù…ÙˆØ²Ø¹Ø©" },
                    Stack = new List<string> { "Algorithms", "Databases", "Software Engineering" },
                    Order = 1,
                    IsActive = true
                }
            };

            context.Credentials.AddRange(credentials);
            return Task.CompletedTask;
        }
    }
}