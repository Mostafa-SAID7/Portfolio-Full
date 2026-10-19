using FluentAssertions;
using FluentValidation.TestHelper;
using PortfolioAPI.Application.Validators;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class ValidatorsTests
    {
        private readonly ProjectValidator _projectValidator;
        private readonly ContactValidator _contactValidator;
        private readonly SkillValidator _skillValidator;

        public ValidatorsTests()
        {
            _projectValidator = new ProjectValidator();
            _contactValidator = new ContactValidator();
            _skillValidator = new SkillValidator();
        }

        #region ProjectValidator Tests

        [Fact]
        public void ProjectValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Project Title",
                Description = "This is a valid project description that meets the length requirements.",
                ImageUrl = "/images/project.png",
                GithubUrl = "https://github.com/user/project",
                LiveUrl = "https://example.com/project",
                Technologies = new List<string> { "C#", ".NET", "React" },
                Category = "Web",
                Client = "Acme",
                Type = "Freelance",
                Database = "SQL Server",
                Status = "Completed",
                Badges = new List<string> { "Featured" },
                Metrics = new List<ProjectMetric> { new() { Label = "Users", Value = "10k" } },
                Gradient = "from-blue-500 to-purple-500"
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleIsEmpty()
        {
            var project = new Project { Title = "", Description = "Valid description" };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleExceedsMaxLength()
        {
            var project = new Project
            {
                Title = new string('A', 101),
                Description = "Valid description"
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Title)
                .WithErrorMessage("Title must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionIsEmpty()
        {
            var project = new Project { Title = "Valid Title", Description = "" };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Description)
                .WithErrorMessage("Description is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionExceedsMaxLength()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = new string('A', 501)
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Description)
                .WithErrorMessage("Description must not exceed 500 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenLiveUrlIsInvalid()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                LiveUrl = "invalid-url"
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.LiveUrl)
                .WithErrorMessage("Live URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenLiveUrlIsNull()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                LiveUrl = null
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldNotHaveValidationErrorFor(p => p.LiveUrl);
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenGithubUrlIsInvalid()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                GithubUrl = "not a url"
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.GithubUrl)
                .WithErrorMessage("GitHub URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenGithubUrlIsNull()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                GithubUrl = null
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldNotHaveValidationErrorFor(p => p.GithubUrl);
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenMoreThanTwentyTechnologies()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                Technologies = Enumerable.Repeat("Tech", 21).ToList()
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Technologies)
                .WithErrorMessage("A project can list at most 20 technologies.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenMoreThanFiveBadges()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                Badges = Enumerable.Repeat("Badge", 6).ToList()
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Badges)
                .WithErrorMessage("A project can have at most 5 badges.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenMoreThanSixMetrics()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                Metrics = Enumerable.Range(1, 7)
                    .Select(i => new ProjectMetric { Label = $"L{i}", Value = $"V{i}" })
                    .ToList()
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.Metrics)
                .WithErrorMessage("A project can show at most 6 metrics.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenImageUrlExceedsMaxLength()
        {
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                ImageUrl = new string('a', 501)
            };

            var result = _projectValidator.TestValidate(project);

            result.ShouldHaveValidationErrorFor(p => p.ImageUrl)
                .WithErrorMessage("Image URL must not exceed 500 characters.");
        }

        #endregion

        #region ContactValidator Tests

        [Fact]
        public void ContactValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Subject = "Project Inquiry",
                Message = "This is a valid message that meets the minimum requirements."
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenNameIsEmpty()
        {
            var contact = new Contact
            {
                Name = "",
                Email = "john@example.com",
                Subject = "Valid Subject",
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenNameExceedsMaxLength()
        {
            var contact = new Contact
            {
                Name = new string('A', 101),
                Email = "john@example.com",
                Subject = "Valid Subject",
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name must not exceed 100 characters.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenEmailIsEmpty()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "",
                Subject = "Valid Subject",
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenEmailIsInvalid()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "invalid-email",
                Subject = "Valid Subject",
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Email must be a valid email address.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenSubjectIsEmpty()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "",
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Subject)
                .WithErrorMessage("Subject is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenSubjectExceedsMaxLength()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = new string('A', 201),
                Message = "Valid message"
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Subject)
                .WithErrorMessage("Subject must not exceed 200 characters.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenMessageIsEmpty()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Valid Subject",
                Message = ""
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Message)
                .WithErrorMessage("Message is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenMessageExceedsMaxLength()
        {
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Valid Subject",
                Message = new string('A', 1001)
            };

            var result = _contactValidator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(c => c.Message)
                .WithErrorMessage("Message must not exceed 1000 characters.");
        }

        #endregion

        #region SkillValidator Tests

        [Fact]
        public void SkillValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            var skill = new Skill
            {
                Name = "C#",
                Category = "Programming",
                Proficiency = 85,
                IconUrl = "https://example.com/csharp.png",
                Description = "Advanced C# development"
            };

            var result = _skillValidator.TestValidate(skill);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenNameIsEmpty()
        {
            var skill = new Skill { Name = "", Category = "Programming", Proficiency = 50 };

            var result = _skillValidator.TestValidate(skill);

            result.ShouldHaveValidationErrorFor(s => s.Name)
                .WithErrorMessage("Skill name is required.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenNameExceedsMaxLength()
        {
            var skill = new Skill
            {
                Name = new string('A', 51),
                Category = "Programming",
                Proficiency = 50
            };

            var result = _skillValidator.TestValidate(skill);

            result.ShouldHaveValidationErrorFor(s => s.Name)
                .WithErrorMessage("Skill name must not exceed 50 characters.");
        }

        [Fact]
        public void SkillValidator_ShouldFail_WhenCategoryIsEmpty()
        {
            var skill = new Skill { Name = "C#", Category = "", Proficiency = 50 };

            var result = _skillValidator.TestValidate(skill);

            result.ShouldHaveValidationErrorFor(s => s.Category)
                .WithErrorMessage("Category is required.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void SkillValidator_ShouldFail_WhenProficiencyIsOutOfRange(int proficiency)
        {
            var skill = new Skill { Name = "C#", Category = "Programming", Proficiency = proficiency };

            var result = _skillValidator.TestValidate(skill);

            result.ShouldHaveValidationErrorFor(s => s.Proficiency)
                .WithErrorMessage("Proficiency must be between 0 and 100.");
        }

        #endregion
    }
}
