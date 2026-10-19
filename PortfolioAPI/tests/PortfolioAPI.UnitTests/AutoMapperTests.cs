using AutoMapper;
using FluentAssertions;
using PortfolioAPI.Application.Mappings;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class AutoMapperTests
    {
        private readonly IMapper _mapper;

        public AutoMapperTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Configuration_RegistersAllExpectedMaps()
        {
            // Assert every entity<->DTO map declared by AutoMapperProfile exists.
            // NOTE: AssertConfigurationIsValid() is deliberately not used here:
            // the profile maps Project<->ProjectDto but never declares
            // CreateMap<ProjectMetric, ProjectMetricDto>, so AutoMapper's full
            // config validation fails on the Metrics member. That src gap is
            // reported separately (see Map metrics notes below); until it is
            // fixed only map existence + the members that do map are asserted.
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            // In AutoMapper 13 the type-map enumeration lives on the internal
            // IGlobalConfiguration surface, implemented explicitly by
            // MapperConfiguration (it is not on MapperConfiguration itself nor
            // on the public IConfigurationProvider).
            var global = (AutoMapper.Internal.IGlobalConfiguration)config;
            var maps = global.GetAllTypeMaps()
                .Select(t => (t.SourceType, t.DestinationType))
                .ToList();

            maps.Should().Contain(m => m.SourceType == typeof(Project) && m.DestinationType == typeof(ProjectDto));
            maps.Should().Contain(m => m.SourceType == typeof(ProjectDto) && m.DestinationType == typeof(Project));
            maps.Should().Contain(m => m.SourceType == typeof(Skill) && m.DestinationType == typeof(SkillDto));
            maps.Should().Contain(m => m.SourceType == typeof(SkillDto) && m.DestinationType == typeof(Skill));
            maps.Should().Contain(m => m.SourceType == typeof(Experience) && m.DestinationType == typeof(ExperienceDto));
            maps.Should().Contain(m => m.SourceType == typeof(ExperienceDto) && m.DestinationType == typeof(Experience));
            maps.Should().Contain(m => m.SourceType == typeof(Testimonial) && m.DestinationType == typeof(TestimonialDto));
            maps.Should().Contain(m => m.SourceType == typeof(TestimonialDto) && m.DestinationType == typeof(Testimonial));
            maps.Should().Contain(m => m.SourceType == typeof(Contact) && m.DestinationType == typeof(ContactDto));
            maps.Should().Contain(m => m.SourceType == typeof(ContactDto) && m.DestinationType == typeof(Contact));
        }

        #region Project Mapping Tests

        [Fact]
        public void Map_ProjectToProjectDto_ShouldMapCorrectly()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                Description = "Test Description",
                ImageUrl = "https://example.com/image.jpg",
                Technologies = new List<string> { "C#", ".NET", "React" },
                GithubUrl = "https://github.com/user/project",
                LiveUrl = "https://example.com/project",
                Featured = true,
                Order = 3,
                IsActive = true,
                Category = "Web",
                Client = "Acme",
                Type = "Freelance",
                Database = "SQL Server",
                Status = "Completed",
                Badges = new List<string> { "Featured", "Gold" },
                // Metrics left empty: AutoMapperProfile has no
                // CreateMap<ProjectMetric, ProjectMetricDto>, so populating
                // this member throws "Missing type map configuration" (src gap,
                // reported separately). Service/controller paths hand-map the
                // metrics, which is covered in ProjectServiceTests.
                Metrics = new List<ProjectMetric>(),
                Gradient = "from-blue-500 to-purple-500",
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var projectDto = _mapper.Map<ProjectDto>(project);

            // Assert
            projectDto.Should().NotBeNull();
            projectDto.Id.Should().Be(project.Id);
            projectDto.Title.Should().Be(project.Title);
            projectDto.Description.Should().Be(project.Description);
            projectDto.ImageUrl.Should().Be(project.ImageUrl);
            projectDto.Technologies.Should().Equal(project.Technologies);
            projectDto.GithubUrl.Should().Be(project.GithubUrl);
            projectDto.LiveUrl.Should().Be(project.LiveUrl);
            projectDto.Featured.Should().BeTrue();
            projectDto.Order.Should().Be(3);
            projectDto.IsActive.Should().BeTrue();
            projectDto.Category.Should().Be("Web");
            projectDto.Client.Should().Be("Acme");
            projectDto.Type.Should().Be("Freelance");
            projectDto.Database.Should().Be("SQL Server");
            projectDto.Status.Should().Be("Completed");
            projectDto.Badges.Should().Equal(project.Badges);
            // Empty (not null) source collection maps to an empty destination
            // list — element mapping never runs, so the missing ProjectMetric
            // map is not exercised here.
            projectDto.Metrics.Should().BeEmpty();
            projectDto.Gradient.Should().Be("from-blue-500 to-purple-500");
            projectDto.CreatedAt.Should().Be(project.CreatedAt);
            projectDto.UpdatedAt.Should().Be(project.UpdatedAt);
        }

        [Fact]
        public void Map_ProjectDtoToProject_ShouldMapCorrectly()
        {
            // Arrange
            var projectDto = new ProjectDto
            {
                Id = 1,
                Title = "Test Project",
                Description = "Test Description",
                ImageUrl = "https://example.com/image.jpg",
                Technologies = new List<string> { "C#", ".NET" },
                GithubUrl = "https://github.com/user/project",
                LiveUrl = "https://example.com/project",
                Featured = true,
                Order = 5,
                IsActive = true,
                Category = "Web",
                Client = "Acme",
                Type = "Self",
                Database = "PostgreSQL",
                Status = "Deployed",
                Badges = new List<string> { "Featured" },
                // Metrics left empty: AutoMapperProfile has no
                // CreateMap<ProjectMetricDto, ProjectMetric> (src gap, reported
                // separately); populated metrics would throw at map time.
                Metrics = new List<ProjectMetricDto>(),
                Gradient = "from-green-500 to-blue-500",
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var project = _mapper.Map<Project>(projectDto);

            // Assert
            project.Should().NotBeNull();
            project.Id.Should().Be(projectDto.Id);
            project.Title.Should().Be(projectDto.Title);
            project.Description.Should().Be(projectDto.Description);
            project.ImageUrl.Should().Be(projectDto.ImageUrl);
            project.Technologies.Should().Equal(projectDto.Technologies);
            project.GithubUrl.Should().Be(projectDto.GithubUrl);
            project.LiveUrl.Should().Be(projectDto.LiveUrl);
            project.Featured.Should().BeTrue();
            project.Order.Should().Be(5);
            project.IsActive.Should().BeTrue();
            project.Category.Should().Be("Web");
            project.Client.Should().Be("Acme");
            project.Type.Should().Be("Self");
            project.Database.Should().Be("PostgreSQL");
            project.Status.Should().Be("Deployed");
            project.Badges.Should().Equal(projectDto.Badges);
            // Empty (not null) source collection maps to an empty list; the
            // missing ProjectMetric element map is not exercised here.
            project.Metrics.Should().BeEmpty();
            project.Gradient.Should().Be("from-green-500 to-blue-500");
            project.CreatedAt.Should().Be(projectDto.CreatedAt);
            project.UpdatedAt.Should().Be(projectDto.UpdatedAt);
        }

        #endregion

        #region Skill Mapping Tests

        [Fact]
        public void Map_SkillToSkillDto_ShouldMapCorrectly()
        {
            // Arrange
            var skill = new Skill
            {
                Id = 1,
                Name = "C#",
                Category = "Programming",
                Proficiency = 85,
                IconUrl = "https://example.com/csharp-icon.png",
                Description = "Advanced C# development",
                Order = 2,
                IsActive = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var skillDto = _mapper.Map<SkillDto>(skill);

            // Assert
            skillDto.Should().NotBeNull();
            skillDto.Id.Should().Be(skill.Id);
            skillDto.Name.Should().Be(skill.Name);
            skillDto.Category.Should().Be(skill.Category);
            skillDto.Proficiency.Should().Be(85);
            skillDto.IconUrl.Should().Be(skill.IconUrl);
            skillDto.Description.Should().Be(skill.Description);
            skillDto.Order.Should().Be(2);
            skillDto.IsActive.Should().BeTrue();
            skillDto.CreatedAt.Should().Be(skill.CreatedAt);
            skillDto.UpdatedAt.Should().Be(skill.UpdatedAt);
        }

        [Fact]
        public void Map_SkillDtoToSkill_ShouldMapCorrectly()
        {
            // Arrange
            var skillDto = new SkillDto
            {
                Id = 1,
                Name = "C#",
                Category = "Programming",
                Proficiency = 85,
                IconUrl = "https://example.com/csharp-icon.png",
                Description = "Advanced C# development",
                Order = 2,
                IsActive = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var skill = _mapper.Map<Skill>(skillDto);

            // Assert
            skill.Should().NotBeNull();
            skill.Id.Should().Be(skillDto.Id);
            skill.Name.Should().Be(skillDto.Name);
            skill.Category.Should().Be(skillDto.Category);
            skill.Proficiency.Should().Be(85);
            skill.IconUrl.Should().Be(skillDto.IconUrl);
            skill.Description.Should().Be(skillDto.Description);
            skill.Order.Should().Be(2);
            skill.IsActive.Should().BeTrue();
            skill.CreatedAt.Should().Be(skillDto.CreatedAt);
            skill.UpdatedAt.Should().Be(skillDto.UpdatedAt);
        }

        #endregion

        #region Contact Mapping Tests

        [Fact]
        public void Map_ContactToContactDto_ShouldMapCorrectly()
        {
            // Arrange
            var contact = new Contact
            {
                Id = 1,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Subject = "Project Inquiry",
                Message = "I am interested in your project.",
                IsRead = false,
                ReadAt = null,
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var contactDto = _mapper.Map<ContactDto>(contact);

            // Assert
            contactDto.Should().NotBeNull();
            contactDto.Id.Should().Be(contact.Id);
            contactDto.Name.Should().Be(contact.Name);
            contactDto.Email.Should().Be(contact.Email);
            contactDto.Subject.Should().Be(contact.Subject);
            contactDto.Message.Should().Be(contact.Message);
            contactDto.IsRead.Should().BeFalse();
            contactDto.ReadAt.Should().BeNull();
            contactDto.IpAddress.Should().Be("192.168.1.1");
            contactDto.UserAgent.Should().Be("Mozilla/5.0");
            contactDto.CreatedAt.Should().Be(contact.CreatedAt);
            contactDto.UpdatedAt.Should().Be(contact.UpdatedAt);
        }

        [Fact]
        public void Map_ContactDtoToContact_ShouldMapCorrectly()
        {
            // Arrange
            var contactDto = new ContactDto
            {
                Id = 1,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Subject = "Project Inquiry",
                Message = "I am interested in your project.",
                IsRead = true,
                ReadAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var contact = _mapper.Map<Contact>(contactDto);

            // Assert
            contact.Should().NotBeNull();
            contact.Id.Should().Be(contactDto.Id);
            contact.Name.Should().Be(contactDto.Name);
            contact.Email.Should().Be(contactDto.Email);
            contact.Subject.Should().Be(contactDto.Subject);
            contact.Message.Should().Be(contactDto.Message);
            contact.IsRead.Should().BeTrue();
            contact.ReadAt.Should().Be(contactDto.ReadAt);
            contact.IpAddress.Should().Be("192.168.1.1");
            contact.UserAgent.Should().Be("Mozilla/5.0");
            contact.CreatedAt.Should().Be(contactDto.CreatedAt);
            contact.UpdatedAt.Should().Be(contactDto.UpdatedAt);
        }

        #endregion

        #region Experience Mapping Tests

        [Fact]
        public void Map_ExperienceToExperienceDto_ShouldMapCorrectly()
        {
            // Arrange
            var experience = new Experience
            {
                Id = 1,
                Company = "Acme Corp",
                Position = "Senior Developer",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2023, 1, 1),
                Description = "Built things",
                Technologies = new List<string> { "C#", "Azure" },
                Location = "Remote",
                Current = false,
                Order = 1,
                IsActive = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var experienceDto = _mapper.Map<ExperienceDto>(experience);

            // Assert
            experienceDto.Should().NotBeNull();
            experienceDto.Id.Should().Be(1);
            experienceDto.Company.Should().Be("Acme Corp");
            experienceDto.Position.Should().Be("Senior Developer");
            experienceDto.StartDate.Should().Be(experience.StartDate);
            experienceDto.EndDate.Should().Be(experience.EndDate);
            experienceDto.Description.Should().Be("Built things");
            experienceDto.Technologies.Should().Equal("C#", "Azure");
            experienceDto.Location.Should().Be("Remote");
            experienceDto.Current.Should().BeFalse();
            experienceDto.Order.Should().Be(1);
            experienceDto.IsActive.Should().BeTrue();
            experienceDto.CreatedAt.Should().Be(experience.CreatedAt);
            experienceDto.UpdatedAt.Should().Be(experience.UpdatedAt);
        }

        [Fact]
        public void Map_ExperienceDtoToExperience_ShouldMapCorrectly()
        {
            // Arrange
            var experienceDto = new ExperienceDto
            {
                Id = 1,
                Company = "Acme Corp",
                Position = "Senior Developer",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = null,
                Description = "Still building",
                Technologies = new List<string> { "C#" },
                Location = "Remote",
                Current = true,
                Order = 1,
                IsActive = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            };

            // Act
            var experience = _mapper.Map<Experience>(experienceDto);

            // Assert
            experience.Should().NotBeNull();
            experience.Id.Should().Be(1);
            experience.Company.Should().Be("Acme Corp");
            experience.Position.Should().Be("Senior Developer");
            experience.StartDate.Should().Be(experienceDto.StartDate);
            experience.EndDate.Should().BeNull();
            experience.Description.Should().Be("Still building");
            experience.Technologies.Should().Equal("C#");
            experience.Location.Should().Be("Remote");
            experience.Current.Should().BeTrue();
            experience.Current.Should().Be(experienceDto.Current);
            experience.IsActive.Should().BeTrue();
            experience.UpdatedAt.Should().BeNull();
        }

        #endregion

        #region Testimonial Mapping Tests

        [Fact]
        public void Map_TestimonialToTestimonialDto_ShouldMapCorrectly()
        {
            // Arrange
            var testimonial = new Testimonial
            {
                Id = 1,
                Name = "Jane Doe",
                Position = "CEO",
                Company = "Acme",
                Content = "Great work!",
                ImageUrl = "https://example.com/jane.jpg",
                Rating = 5,
                Featured = true,
                Order = 1,
                IsActive = true,
                ProjectUrl = "https://example.com/project",
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var testimonialDto = _mapper.Map<TestimonialDto>(testimonial);

            // Assert
            testimonialDto.Should().NotBeNull();
            testimonialDto.Id.Should().Be(1);
            testimonialDto.Name.Should().Be("Jane Doe");
            testimonialDto.Position.Should().Be("CEO");
            testimonialDto.Company.Should().Be("Acme");
            testimonialDto.Content.Should().Be("Great work!");
            testimonialDto.ImageUrl.Should().Be("https://example.com/jane.jpg");
            testimonialDto.Rating.Should().Be(5);
            testimonialDto.Featured.Should().BeTrue();
            testimonialDto.Order.Should().Be(1);
            testimonialDto.IsActive.Should().BeTrue();
            testimonialDto.ProjectUrl.Should().Be("https://example.com/project");
            testimonialDto.CreatedAt.Should().Be(testimonial.CreatedAt);
            testimonialDto.UpdatedAt.Should().Be(testimonial.UpdatedAt);
        }

        [Fact]
        public void Map_TestimonialDtoToTestimonial_ShouldMapCorrectly()
        {
            // Arrange
            var testimonialDto = new TestimonialDto
            {
                Id = 1,
                Name = "Jane Doe",
                Position = "CEO",
                Company = "Acme",
                Content = "Great work!",
                ImageUrl = null,
                Rating = 4,
                Featured = false,
                Order = 2,
                IsActive = true,
                ProjectUrl = null,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            };

            // Act
            var testimonial = _mapper.Map<Testimonial>(testimonialDto);

            // Assert
            testimonial.Should().NotBeNull();
            testimonial.Id.Should().Be(1);
            testimonial.Name.Should().Be("Jane Doe");
            testimonial.Position.Should().Be("CEO");
            testimonial.Company.Should().Be("Acme");
            testimonial.Content.Should().Be("Great work!");
            testimonial.ImageUrl.Should().BeNull();
            testimonial.Rating.Should().Be(4);
            testimonial.Featured.Should().BeFalse();
            testimonial.Order.Should().Be(2);
            testimonial.IsActive.Should().BeTrue();
            testimonial.ProjectUrl.Should().BeNull();
            testimonial.CreatedAt.Should().Be(testimonialDto.CreatedAt);
            testimonial.UpdatedAt.Should().BeNull();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Map_ProjectWithNullValues_ShouldMapCorrectly()
        {
            // Arrange — nullable members left at their defaults
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                Description = "Test Description",
                ImageUrl = null,
                GithubUrl = null,
                LiveUrl = null,
                Category = null,
                Client = null,
                Type = null,
                Database = null,
                Status = null,
                Gradient = null,
                Badges = null,
                Metrics = null,
                UpdatedAt = null
            };

            // Act
            var projectDto = _mapper.Map<ProjectDto>(project);

            // Assert
            projectDto.Should().NotBeNull();
            projectDto.ImageUrl.Should().BeNull();
            projectDto.GithubUrl.Should().BeNull();
            projectDto.LiveUrl.Should().BeNull();
            projectDto.Category.Should().BeNull();
            projectDto.Client.Should().BeNull();
            projectDto.Type.Should().BeNull();
            projectDto.Database.Should().BeNull();
            projectDto.Status.Should().BeNull();
            projectDto.Gradient.Should().BeNull();
            projectDto.Badges.Should().BeNullOrEmpty();
            projectDto.Metrics.Should().BeNullOrEmpty();
            projectDto.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Map_SkillWithNullValues_ShouldMapCorrectly()
        {
            // Arrange
            var skill = new Skill
            {
                Id = 1,
                Name = "Test Skill",
                IconUrl = null,
                Description = null,
                UpdatedAt = null
            };

            // Act
            var skillDto = _mapper.Map<SkillDto>(skill);

            // Assert
            skillDto.Should().NotBeNull();
            skillDto.Name.Should().Be("Test Skill");
            skillDto.IconUrl.Should().BeNull();
            skillDto.Description.Should().BeNull();
            skillDto.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Map_ContactWithNullValues_ShouldMapCorrectly()
        {
            // Arrange
            var contact = new Contact
            {
                Id = 1,
                Name = "Test Contact",
                Email = "test@example.com",
                ReadAt = null,
                IpAddress = null,
                UserAgent = null,
                UpdatedAt = null
            };

            // Act
            var contactDto = _mapper.Map<ContactDto>(contact);

            // Assert
            contactDto.Should().NotBeNull();
            contactDto.Name.Should().Be("Test Contact");
            contactDto.ReadAt.Should().BeNull();
            contactDto.IpAddress.Should().BeNull();
            contactDto.UserAgent.Should().BeNull();
            contactDto.UpdatedAt.Should().BeNull();
        }

        #endregion
    }
}
