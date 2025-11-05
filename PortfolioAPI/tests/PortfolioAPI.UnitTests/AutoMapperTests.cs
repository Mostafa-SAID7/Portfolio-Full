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
        public void Configuration_ShouldBeValid()
        {
            // Act & Assert
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
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
                Technologies = "C#, .NET",
                ProjectUrl = "https://example.com/project",
                RepositoryUrl = "https://github.com/user/project",
                ImageUrl = "https://example.com/image.jpg",
                IsFeatured = true,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2023, 12, 31),
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var projectDto = _mapper.Map<ProjectDto>(project);

            // Assert
            projectDto.Should().NotBeNull();
            projectDto.Id.Should().Be(project.Id);
            projectDto.Title.Should().Be(project.Title);
            projectDto.Description.Should().Be(project.Description);
            projectDto.Technologies.Should().Be(project.Technologies);
            projectDto.ProjectUrl.Should().Be(project.ProjectUrl);
            projectDto.RepositoryUrl.Should().Be(project.RepositoryUrl);
            projectDto.ImageUrl.Should().Be(project.ImageUrl);
            projectDto.IsFeatured.Should().Be(project.IsFeatured);
            projectDto.StartDate.Should().Be(project.StartDate);
            projectDto.EndDate.Should().Be(project.EndDate);
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
                Technologies = "C#, .NET",
                ProjectUrl = "https://example.com/project",
                RepositoryUrl = "https://github.com/user/project",
                ImageUrl = "https://example.com/image.jpg",
                IsFeatured = true,
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2023, 12, 31),
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var project = _mapper.Map<Project>(projectDto);

            // Assert
            project.Should().NotBeNull();
            project.Id.Should().Be(projectDto.Id);
            project.Title.Should().Be(projectDto.Title);
            project.Description.Should().Be(projectDto.Description);
            project.Technologies.Should().Be(projectDto.Technologies);
            project.ProjectUrl.Should().Be(projectDto.ProjectUrl);
            project.RepositoryUrl.Should().Be(projectDto.RepositoryUrl);
            project.ImageUrl.Should().Be(projectDto.ImageUrl);
            project.IsFeatured.Should().Be(projectDto.IsFeatured);
            project.StartDate.Should().Be(projectDto.StartDate);
            project.EndDate.Should().Be(projectDto.EndDate);
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
                ProficiencyLevel = 85,
                IconUrl = "https://example.com/csharp-icon.png",
                IsVisible = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var skillDto = _mapper.Map<SkillDto>(skill);

            // Assert
            skillDto.Should().NotBeNull();
            skillDto.Id.Should().Be(skill.Id);
            skillDto.Name.Should().Be(skill.Name);
            skillDto.Category.Should().Be(skill.Category);
            skillDto.ProficiencyLevel.Should().Be(skill.ProficiencyLevel);
            skillDto.IconUrl.Should().Be(skill.IconUrl);
            skillDto.IsVisible.Should().Be(skill.IsVisible);
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
                ProficiencyLevel = 85,
                IconUrl = "https://example.com/csharp-icon.png",
                IsVisible = true,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var skill = _mapper.Map<Skill>(skillDto);

            // Assert
            skill.Should().NotBeNull();
            skill.Id.Should().Be(skillDto.Id);
            skill.Name.Should().Be(skillDto.Name);
            skill.Category.Should().Be(skillDto.Category);
            skill.ProficiencyLevel.Should().Be(skillDto.ProficiencyLevel);
            skill.IconUrl.Should().Be(skillDto.IconUrl);
            skill.IsVisible.Should().Be(skillDto.IsVisible);
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
                Phone = "+1234567890",
                Subject = "Project Inquiry",
                Message = "I am interested in your project.",
                IsRead = false,
                ReadAt = null,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var contactDto = _mapper.Map<ContactDto>(contact);

            // Assert
            contactDto.Should().NotBeNull();
            contactDto.Id.Should().Be(contact.Id);
            contactDto.Name.Should().Be(contact.Name);
            contactDto.Email.Should().Be(contact.Email);
            contactDto.Phone.Should().Be(contact.Phone);
            contactDto.Subject.Should().Be(contact.Subject);
            contactDto.Message.Should().Be(contact.Message);
            contactDto.IsRead.Should().Be(contact.IsRead);
            contactDto.ReadAt.Should().Be(contact.ReadAt);
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
                Phone = "+1234567890",
                Subject = "Project Inquiry",
                Message = "I am interested in your project.",
                IsRead = false,
                ReadAt = null,
                CreatedAt = new DateTime(2023, 1, 1, 10, 0, 0),
                UpdatedAt = new DateTime(2023, 6, 1, 10, 0, 0)
            };

            // Act
            var contact = _mapper.Map<Contact>(contactDto);

            // Assert
            contact.Should().NotBeNull();
            contact.Id.Should().Be(contactDto.Id);
            contact.Name.Should().Be(contactDto.Name);
            contact.Email.Should().Be(contactDto.Email);
            contact.Phone.Should().Be(contactDto.Phone);
            contact.Subject.Should().Be(contactDto.Subject);
            contact.Message.Should().Be(contactDto.Message);
            contact.IsRead.Should().Be(contactDto.IsRead);
            contact.ReadAt.Should().Be(contactDto.ReadAt);
            contact.CreatedAt.Should().Be(contactDto.CreatedAt);
            contact.UpdatedAt.Should().Be(contactDto.UpdatedAt);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Map_ProjectWithNullValues_ShouldMapCorrectly()
        {
            // Arrange
            var project = new Project
            {
                Id = 1,
                Title = "Test Project",
                Description = "Test Description",
                Technologies = null,
                ProjectUrl = null,
                RepositoryUrl = null,
                ImageUrl = null,
                StartDate = null,
                EndDate = null,
                UpdatedAt = null
            };

            // Act
            var projectDto = _mapper.Map<ProjectDto>(project);

            // Assert
            projectDto.Should().NotBeNull();
            projectDto.Technologies.Should().BeNull();
            projectDto.ProjectUrl.Should().BeNull();
            projectDto.RepositoryUrl.Should().BeNull();
            projectDto.ImageUrl.Should().BeNull();
            projectDto.StartDate.Should().BeNull();
            projectDto.EndDate.Should().BeNull();
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
                Category = null,
                IconUrl = null,
                UpdatedAt = null
            };

            // Act
            var skillDto = _mapper.Map<SkillDto>(skill);

            // Assert
            skillDto.Should().NotBeNull();
            skillDto.Category.Should().BeNull();
            skillDto.IconUrl.Should().BeNull();
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
                Phone = null,
                Subject = null,
                ReadAt = null,
                UpdatedAt = null
            };

            // Act
            var contactDto = _mapper.Map<ContactDto>(contact);

            // Assert
            contactDto.Should().NotBeNull();
            contactDto.Phone.Should().BeNull();
            contactDto.Subject.Should().BeNull();
            contactDto.ReadAt.Should().BeNull();
            contactDto.UpdatedAt.Should().BeNull();
        }

        #endregion
    }
}