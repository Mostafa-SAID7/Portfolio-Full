using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(PortfolioDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if data already exists
            if (context.Projects.Any() || context.Skills.Any())
            {
                return; // Database has been seeded
            }

            // Seed initial data
            SeedSkills(context);
            SeedProjects(context);
            SeedExperiences(context);
            SeedTestimonials(context);

            context.SaveChanges();
        }

        private static void SeedSkills(PortfolioDbContext context)
        {
            var skills = new[]
            {
                new Skill { Name = "C#", Category = "Programming Languages", ProficiencyLevel = 90, IsVisible = true },
                new Skill { Name = ".NET Core", Category = "Frameworks", ProficiencyLevel = 85, IsVisible = true },
                new Skill { Name = "Entity Framework", Category = "ORM", ProficiencyLevel = 80, IsVisible = true },
                new Skill { Name = "SQL Server", Category = "Databases", ProficiencyLevel = 75, IsVisible = true },
                new Skill { Name = "JavaScript", Category = "Programming Languages", ProficiencyLevel = 85, IsVisible = true },
                new Skill { Name = "React", Category = "Frontend Frameworks", ProficiencyLevel = 80, IsVisible = true },
                new Skill { Name = "HTML/CSS", Category = "Web Technologies", ProficiencyLevel = 90, IsVisible = true },
                new Skill { Name = "Git", Category = "Version Control", ProficiencyLevel = 85, IsVisible = true },
                new Skill { Name = "Azure", Category = "Cloud Platforms", ProficiencyLevel = 70, IsVisible = true },
                new Skill { Name = "Docker", Category = "Containerization", ProficiencyLevel = 65, IsVisible = true }
            };

            context.Skills.AddRange(skills);
        }

        private static void SeedProjects(PortfolioDbContext context)
        {
            var projects = new[]
            {
                new Project
                {
                    Title = "Portfolio API",
                    Description = "A comprehensive REST API for managing portfolio data including projects, skills, and contact information.",
                    Technologies = "C#, .NET Core, Entity Framework, SQL Server",
                    ProjectUrl = "https://github.com/username/portfolio-api",
                    RepositoryUrl = "https://github.com/username/portfolio-api",
                    IsFeatured = true,
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 6, 1)
                },
                new Project
                {
                    Title = "E-commerce Platform",
                    Description = "A full-stack e-commerce solution with user authentication, product management, and payment integration.",
                    Technologies = "React, Node.js, MongoDB, Stripe API",
                    ProjectUrl = "https://example-ecommerce.com",
                    RepositoryUrl = "https://github.com/username/ecommerce-platform",
                    IsFeatured = true,
                    StartDate = new DateTime(2023, 6, 1),
                    EndDate = new DateTime(2023, 12, 1)
                },
                new Project
                {
                    Title = "Task Management App",
                    Description = "A collaborative task management application with real-time updates and team collaboration features.",
                    Technologies = "Vue.js, Firebase, Tailwind CSS",
                    ProjectUrl = "https://taskmanager-demo.com",
                    RepositoryUrl = "https://github.com/username/task-manager",
                    IsFeatured = false,
                    StartDate = new DateTime(2023, 3, 1),
                    EndDate = new DateTime(2023, 5, 1)
                }
            };

            context.Projects.AddRange(projects);
        }

        private static void SeedExperiences(PortfolioDbContext context)
        {
            var experiences = new[]
            {
                new Experience
                {
                    JobTitle = "Senior Software Developer",
                    CompanyName = "Tech Solutions Inc.",
                    Location = "New York, NY",
                    StartDate = new DateTime(2022, 1, 1),
                    IsCurrentPosition = true,
                    Description = "Leading development of enterprise applications using .NET technologies. Responsible for architecture design, code reviews, and mentoring junior developers.",
                    Technologies = "C#, .NET Core, Azure, SQL Server, React"
                },
                new Experience
                {
                    JobTitle = "Software Developer",
                    CompanyName = "Digital Innovations Ltd.",
                    Location = "San Francisco, CA",
                    StartDate = new DateTime(2020, 6, 1),
                    EndDate = new DateTime(2021, 12, 31),
                    Description = "Developed and maintained web applications using modern JavaScript frameworks. Collaborated with cross-functional teams to deliver high-quality software solutions.",
                    Technologies = "JavaScript, React, Node.js, MongoDB"
                },
                new Experience
                {
                    JobTitle = "Junior Developer",
                    CompanyName = "StartupXYZ",
                    Location = "Austin, TX",
                    StartDate = new DateTime(2019, 3, 1),
                    EndDate = new DateTime(2020, 5, 31),
                    Description = "Contributed to the development of mobile and web applications. Gained experience in agile development practices and version control systems.",
                    Technologies = "C#, ASP.NET, JavaScript, Git"
                }
            };

            context.Experiences.AddRange(experiences);
        }

        private static void SeedTestimonials(PortfolioDbContext context)
        {
            var testimonials = new[]
            {
                new Testimonial
                {
                    ClientName = "John Smith",
                    ClientPosition = "CTO",
                    ClientCompany = "TechCorp",
                    Content = "Excellent work on our e-commerce platform. The solution was delivered on time and exceeded our expectations in terms of performance and scalability.",
                    Rating = 5,
                    IsVisible = true,
                    GivenDate = new DateTime(2024, 1, 15)
                },
                new Testimonial
                {
                    ClientName = "Sarah Johnson",
                    ClientPosition = "Project Manager",
                    ClientCompany = "InnovateNow",
                    Content = "Professional, reliable, and highly skilled developer. Great communication throughout the project and delivered quality code.",
                    Rating = 5,
                    IsVisible = true,
                    GivenDate = new DateTime(2023, 11, 20)
                }
            };

            context.Testimonials.AddRange(testimonials);
        }
    }
}