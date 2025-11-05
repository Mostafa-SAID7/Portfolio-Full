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

        public ValidatorsTests()
        {
            _projectValidator = new ProjectValidator();
            _contactValidator = new ContactValidator();
        }

        #region ProjectValidator Tests

        [Fact]
        public void ProjectValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Project Title",
                Description = "This is a valid project description that meets the minimum length requirements.",
                ProjectUrl = "https://github.com/user/project",
                RepositoryUrl = "https://github.com/user/repo",
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2023, 12, 31)
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleIsEmpty()
        {
            // Arrange
            var project = new Project
            {
                Title = "",
                Description = "Valid description"
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenTitleExceedsMaxLength()
        {
            // Arrange
            var project = new Project
            {
                Title = new string('A', 101), // 101 characters
                Description = "Valid description"
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.Title)
                .WithErrorMessage("Title must not exceed 100 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionIsEmpty()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = ""
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.Description)
                .WithErrorMessage("Description is required.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenDescriptionExceedsMaxLength()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = new string('A', 501) // 501 characters
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.Description)
                .WithErrorMessage("Description must not exceed 500 characters.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenProjectUrlIsInvalid()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                ProjectUrl = "invalid-url"
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.ProjectUrl)
                .WithErrorMessage("Project URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenProjectUrlIsNull()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                ProjectUrl = null
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldNotHaveValidationErrorFor(p => p.ProjectUrl);
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenRepositoryUrlIsInvalid()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                RepositoryUrl = "invalid-url"
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.RepositoryUrl)
                .WithErrorMessage("Repository URL must be a valid URL.");
        }

        [Fact]
        public void ProjectValidator_ShouldFail_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                StartDate = new DateTime(2023, 12, 31),
                EndDate = new DateTime(2023, 1, 1)
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldHaveValidationErrorFor(p => p.StartDate)
                .WithErrorMessage("Start date must be before or equal to end date.");
        }

        [Fact]
        public void ProjectValidator_ShouldPass_WhenStartDateEqualsEndDate()
        {
            // Arrange
            var project = new Project
            {
                Title = "Valid Title",
                Description = "Valid description",
                StartDate = new DateTime(2023, 1, 1),
                EndDate = new DateTime(2023, 1, 1)
            };

            // Act
            var result = _projectValidator.TestValidate(project);

            // Assert
            result.ShouldNotHaveValidationErrorFor(p => p.StartDate);
        }

        #endregion

        #region ContactValidator Tests

        [Fact]
        public void ContactValidator_ShouldPass_WhenAllFieldsAreValid()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Phone = "+1234567890",
                Subject = "Valid Subject",
                Message = "This is a valid message that meets the minimum requirements."
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenNameIsEmpty()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "",
                Email = "john@example.com",
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenNameExceedsMaxLength()
        {
            // Arrange
            var contact = new Contact
            {
                Name = new string('A', 101), // 101 characters
                Email = "john@example.com",
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Name)
                .WithErrorMessage("Name must not exceed 100 characters.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenEmailIsEmpty()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "",
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenEmailIsInvalid()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "invalid-email",
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Email)
                .WithErrorMessage("Email must be a valid email address.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenPhoneExceedsMaxLength()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = new string('1', 16), // 16 characters
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Phone)
                .WithErrorMessage("Phone number must not exceed 15 characters.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenPhoneFormatIsInvalid()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "123456789", // Invalid format (no country code)
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Phone)
                .WithErrorMessage("Phone number must be a valid format.");
        }

        [Fact]
        public void ContactValidator_ShouldPass_WhenPhoneIsNull()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = null,
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldNotHaveValidationErrorFor(c => c.Phone);
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenSubjectExceedsMaxLength()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = new string('A', 201), // 201 characters
                Message = "Valid message"
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Subject)
                .WithErrorMessage("Subject must not exceed 200 characters.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenMessageIsEmpty()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Message = ""
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Message)
                .WithErrorMessage("Message is required.");
        }

        [Fact]
        public void ContactValidator_ShouldFail_WhenMessageExceedsMaxLength()
        {
            // Arrange
            var contact = new Contact
            {
                Name = "John Doe",
                Email = "john@example.com",
                Message = new string('A', 1001) // 1001 characters
            };

            // Act
            var result = _contactValidator.TestValidate(contact);

            // Assert
            result.ShouldHaveValidationErrorFor(c => c.Message)
                .WithErrorMessage("Message must not exceed 1000 characters.");
        }

        #endregion
    }
}