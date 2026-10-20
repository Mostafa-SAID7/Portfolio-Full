using System;
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
            // Only seed reference data in non-production environments (issue #40)
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

            try
            {
                await EnsureSchemaCompatibilityAsync(context);
            }
            catch (Exception ex)
            {
                // Log but don't fail - schema compatibility is best effort
                // The app can still function if the schema is already compatible
            }

            if (!isProduction)
            {
                try
                {
                    await SeedReferenceDataAsync(context);
                }
                catch (Exception ex)
                {
                    // Log but don't fail - seeding is best effort
                }
            }
        }

        private static async Task EnsureSchemaCompatibilityAsync(PortfolioDbContext context)
        {
            // The Category column on Projects was added by migration.
            // If the column is missing (extremely old DB), add it.
            try
            {
                if (!await ColumnExistsAsync(context, "Projects", "Category"))
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE dbo.Projects
                        ADD Category nvarchar(100) NULL;
                    ");
                }
            }
            catch (Exception)
            {
                // Column might already exist or permissions insufficient - continue
            }

            // The SkillProject join table was added by migration.
            // If it's missing, create it.
            try
            {
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
            }
            catch (Exception)
            {
                // Table might already exist or permissions insufficient - continue
            }

            // Sync categories from projects - registers every category the projects
            // already use so the dashboard can manage them immediately.
            try
            {
                await SyncCategoriesAsync(context);
            }
            catch (Exception)
            {
                // Sync failed - continue
            }
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
            try
            {
                var count = await context.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'")
                    .SingleAsync();
                return count > 0;
            }
            catch (Exception)
            {
                // Assume column exists if we can't check
                return true;
            }
        }

        private static async Task<bool> TableExistsAsync(PortfolioDbContext context, string table)
        {
            try
            {
                var count = await context.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'")
                    .SingleAsync();
                return count > 0;
            }
            catch (Exception)
            {
                // Assume table exists if we can't check
                return true;
            }
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
                new Category { NameEn = "Clothing", NameAr = "ملابس", Order = 1, IsActive = true },
                new Category { NameEn = "Beauty", NameAr = "جمال", Order = 2, IsActive = true },
                new Category { NameEn = "Jewelry", NameAr = "مجوهرات", Order = 3, IsActive = true },
                new Category { NameEn = "Furniture", NameAr = "أثاث", Order = 4, IsActive = true },
                new Category { NameEn = "Dashboard", NameAr = "لوحة تحكم", Order = 5, IsActive = true },
                new Category { NameEn = "Web3", NameAr = "ويب 3", Order = 6, IsActive = true },
                new Category { NameEn = "Sports", NameAr = "رياضة", Order = 7, IsActive = true },
                new Category { NameEn = "Tech Accessories", NameAr = "إكسسوارات تقنية", Order = 8, IsActive = true },
                new Category { NameEn = "Backend", NameAr = "باك إند", Order = 9, IsActive = true },
                new Category { NameEn = "Other", NameAr = "أخرى", Order = 10, IsActive = true }
            };

            context.Categories.AddRange(categories);
            return Task.CompletedTask;
        }

        private static Task SeedSkills(PortfolioDbContext context)
        {
            var skills = new[]
            {
                new Skill { NameEn = "C#", NameAr = "سي شارب", CategoryEn = "Programming Languages", CategoryAr = "لغات البرمجة", Proficiency = 90, IsActive = true, Order = 1 },
                new Skill { NameEn = ".NET", NameAr = "دوت نت", CategoryEn = "Frameworks", CategoryAr = "أطر العمل", Proficiency = 88, IsActive = true, Order = 2 },
                new Skill { NameEn = "ASP.NET Core", NameAr = "إيه إس بي دوت نت كور", CategoryEn = "Frameworks", CategoryAr = "أطر العمل", Proficiency = 85, IsActive = true, Order = 3 },
                new Skill { NameEn = "Entity Framework", NameAr = "إنتتي فريمورك", CategoryEn = "Data Access", CategoryAr = "الوصول للبيانات", Proficiency = 82, IsActive = true, Order = 4 },
                new Skill { NameEn = "SQL Server", NameAr = "إس كيو إل سيرفر", CategoryEn = "Databases", CategoryAr = "قواعد البيانات", Proficiency = 80, IsActive = true, Order = 4 },
                new Skill { NameEn = "Angular", NameAr = "أنجولار", CategoryEn = "Frontend", CategoryAr = "الواجهة الأمامية", Proficiency = 85, IsActive = true, Order = 5 },
                new Skill { NameEn = "React", NameAr = "رياكت", CategoryEn = "Frontend", CategoryAr = "الواجهة الأمامية", Proficiency = 80, IsActive = true, Order = 6 },
                new Skill { NameEn = "TypeScript", NameAr = "تايب سكريبت", CategoryEn = "Programming Languages", CategoryAr = "لغات البرمجة", Proficiency = 82, IsActive = true, Order = 7 },
                new Skill { NameEn = "JavaScript", NameAr = "جافا سكريبت", CategoryEn = "Programming Languages", CategoryAr = "لغات البرمجة", Proficiency = 85, IsActive = true, Order = 6 },
                new Skill { NameEn = "HTML/CSS", NameAr = "إتش تي إم إل / سي إس إس", CategoryEn = "Web Technologies", CategoryAr = "تقنيات الويب", Proficiency = 90, IsActive = true, Order = 7 },
                new Skill { NameEn = "Git", NameAr = "جيت", CategoryEn = "Tools", CategoryAr = "الأدوات", Proficiency = 85, IsActive = true, Order = 8 },
                new Skill { NameEn = "Docker", NameAr = "دوكير", CategoryEn = "DevOps", CategoryAr = "ديف أوبس", Proficiency = 70, IsActive = true, Order = 9 },
                new Skill { NameEn = "Azure", NameAr = "أزور", CategoryEn = "Cloud", CategoryAr = "السحابة", Proficiency = 72, IsActive = true, Order = 10 }
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
                    TitleAr = "منصة المحفظة",
                    DescriptionEn = "Full portfolio suite: .NET 9 REST API, Angular admin dashboard and a TanStack Start public site, all driven by one centralized backend.",
                    DescriptionAr = "مجموعة محفظة كاملة: .NET 9 REST API، لوحة تحكم أنجلار الإدارية وموقع عام TanStack Start، كلها مدعومة بخلفية مركزية واحدة.",
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
                    TitleAr = "نظام إدارة المخزون",
                    DescriptionEn = "E-Inventory platform for tracking stock levels, suppliers and purchase orders with role-based access and reporting.",
                    DescriptionAr = "منصة E-Inventory لتتبع مستويات المخزون والموردين وأوامر الشراء مع وصول يعتمد على الأدوار والتقارير.",
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
                    TitleAr = "تطبيق إدارة المهام",
                    DescriptionEn = "A collaborative task management application with real-time updates and team collaboration features.",
                    DescriptionAr = "تطبيق إدارة مهام تعاوني مع تحديثات في الوقت الفعلي وميزات تعاون الفريق.",
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
                    PositionAr = "مطور برمجيات",
                    CompanyEn = "Freelance",
                    CompanyAr = "عمل حر",
                    LocationEn = "Remote",
                    LocationAr = "عن بعد",
                    StartDate = new DateTime(2023, 1, 1),
                    Current = true,
                    DescriptionEn = "Building full-stack web applications: .NET APIs, Angular/React frontends and SQL Server databases for clients.",
                    DescriptionAr = "بناء تطبيقات ويب كاملة: واجهات .NET APIs، وواجهات أمامية Angular/React وقواعد بيانات SQL Server للعملاء.",
                    Technologies = new List<string> { ".NET", "Angular", "React", "SQL Server" },
                    Order = 1,
                    IsActive = true
                },
                new Experience
                {
                    PositionEn = "Junior Software Developer",
                    PositionAr = "مطور برمجيات مبتدئ",
                    CompanyEn = "Tech Solutions Inc.",
                    CompanyAr = "تك سوليوشنز",
                    LocationEn = "Cairo, EG",
                    LocationAr = "القاهرة، مصر",
                    StartDate = new DateTime(2021, 6, 1),
                    EndDate = new DateTime(2022, 12, 31),
                    Current = false,
                    DescriptionEn = "Developed and maintained web applications using .NET technologies and modern JavaScript frameworks.",
                    DescriptionAr = "تطوير وصيانة تطبيقات الويب باستخدام تقنيات .NET وأطر عمل JavaScript الحديثة.",
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
                    PeriodAr = "2023 - الحاضر",
                    TitleEn = "Software Developer",
                    TitleAr = "مطور برمجيات",
                    OrgEn = "Freelance",
                    OrgAr = "عمل حر",
                    LocationEn = "Remote",
                    LocationAr = "عن بعد",
                    SummaryEn = "Building full-stack web applications: .NET APIs, Angular/React frontends and SQL Server databases for clients.",
                    SummaryAr = "بناء تطبيقات ويب كاملة: واجهات .NET APIs، وواجهات أمامية Angular/React وقواعد بيانات SQL Server للعملاء.",
                    HighlightsEn = new List<string> { "Designed and implemented REST APIs", "Optimized SQL queries for 50% performance improvement" },
                    HighlightsAr = new List<string> { "تصميم وتنفيذ واجهات REST APIs", "تحسين استعلامات SQL لتحسين الأداء بنسبة 50%" },
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
                    TitleAr = "بكالوريوس علوم الحاسب",
                    OrgEn = "University of Technology",
                    OrgAr = "جامعة التكنولوجيا",
                    LocationEn = "Cairo, Egypt",
                    LocationAr = "القاهرة، مصر",
                    SummaryEn = "Focused on software engineering, algorithms and database systems.",
                    SummaryAr = "التركيز على هندسة البرمجيات، الخوارزميات وأنظمة قواعد البيانات.",
                    HighlightsEn = new List<string> { "Graduated with honors", "Final project: Distributed Systems" },
                    HighlightsAr = new List<string> { "تخرج بتفوق", "مشروع التخرج: الأنظمة الموزعة" },
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