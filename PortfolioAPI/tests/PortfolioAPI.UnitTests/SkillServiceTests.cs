using FluentAssertions;
using Moq;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Services;
using Xunit;
using FluentValidation;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.UnitTests
{
    public class SkillServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<Skill>> _validatorMock;
        private readonly SkillService _skillService;

        public SkillServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<Skill>>();
            _skillService = new SkillService(_unitOfWorkMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task GetAllSkillsAsync_ShouldReturnAllSkills()
        {
            // Arrange
            var skills = new List<Skill>
            {
                new Skill { Id = 1, Name = "C#", Category = "Programming" },
                new Skill { Id = 2, Name = "JavaScript", Category = "Programming" }
            };
            _unitOfWorkMock.Setup(u => u.Skills.GetAllAsync()).ReturnsAsync(skills);

            // Act
            var result = await _skillService.GetAllSkillsAsync();

            // Assert
            result.Should().BeEquivalentTo(skills);
            _unitOfWorkMock.Verify(u => u.Skills.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetSkillByIdAsync_ShouldReturnSkill_WhenSkillExists()
        {
            // Arrange
            var skill = new Skill { Id = 1, Name = "C#", Category = "Programming" };
            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync(skill);

            // Act
            var result = await _skillService.GetSkillByIdAsync(1);

            // Assert
            result.Should().Be(skill);
            _unitOfWorkMock.Verify(u => u.Skills.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetSkillByIdAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync((Skill?)null);

            // Act
            Func<Task> act = async () => await _skillService.GetSkillByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Skill with id 1 not found.");
        }

        [Fact]
        public async Task GetSkillsByCategoryAsync_ShouldReturnSkillsByCategory()
        {
            // Arrange
            var skills = new List<Skill>
            {
                new Skill { Id = 1, Name = "C#", Category = "Programming" },
                new Skill { Id = 2, Name = "SQL", Category = "Database" },
                new Skill { Id = 3, Name = "JavaScript", Category = "Programming" }
            };
            _unitOfWorkMock.Setup(u => u.Skills.FindAsync(s => s.Category == "Programming"))
                .ReturnsAsync(skills.Where(s => s.Category == "Programming"));

            // Act
            var result = await _skillService.GetSkillsByCategoryAsync("Programming");

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(s => s.Category.Should().Be("Programming"));
            _unitOfWorkMock.Verify(u => u.Skills.FindAsync(s => s.Category == "Programming"), Times.Once);
        }

        [Fact]
        public async Task GetVisibleSkillsAsync_ShouldReturnVisibleSkills()
        {
            // Arrange
            var skills = new List<Skill>
            {
                new Skill { Id = 1, Name = "C#", IsActive = true },
                new Skill { Id = 2, Name = "Hidden Skill", IsActive = false }
            };
            _unitOfWorkMock.Setup(u => u.Skills.FindAsync(s => s.IsActive))
                .ReturnsAsync(skills.Where(s => s.IsActive));

            // Act
            var result = await _skillService.GetVisibleSkillsAsync();

            // Assert
            result.Should().ContainSingle(s => s.IsActive);
            _unitOfWorkMock.Verify(u => u.Skills.FindAsync(s => s.IsActive), Times.Once);
        }

        [Fact]
        public async Task CreateSkillAsync_ShouldCreateSkill_WhenValidationPasses()
        {
            // Arrange
            var skill = new Skill { Name = "New Skill", Category = "New Category" };
            var validationResult = new FluentValidation.Results.ValidationResult();
            var createdSkill = new Skill { Id = 1, Name = "New Skill", Category = "New Category", CreatedAt = DateTime.UtcNow };

            _validatorMock.Setup(v => v.ValidateAsync(skill, default)).ReturnsAsync(validationResult);
            _unitOfWorkMock.Setup(u => u.Skills.AddAsync(It.IsAny<Skill>())).ReturnsAsync(createdSkill);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _skillService.CreateSkillAsync(skill);

            // Assert
            result.Should().Be(createdSkill);
            _validatorMock.Verify(v => v.ValidateAsync(skill, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Skills.AddAsync(It.IsAny<Skill>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateSkillAsync_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var skill = new Skill { Name = "", Category = "" };
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Name", "Name is required.")
            };
            var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);

            _validatorMock.Setup(v => v.ValidateAsync(skill, default)).ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _skillService.CreateSkillAsync(skill);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _validatorMock.Verify(v => v.ValidateAsync(skill, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Skills.AddAsync(It.IsAny<Skill>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldUpdateSkill_WhenSkillExistsAndValidationPasses()
        {
            // Arrange
            var skill = new Skill { Id = 1, Name = "Updated Skill", Category = "Updated Category", Proficiency = 90 };
            var existingSkill = new Skill { Id = 1, Name = "Old Skill", Category = "Old Category" };
            var validationResult = new FluentValidation.Results.ValidationResult();

            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync(existingSkill);
            _validatorMock.Setup(v => v.ValidateAsync(existingSkill, default)).ReturnsAsync(validationResult);
            _unitOfWorkMock.Setup(u => u.Skills.UpdateAsync(existingSkill)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _skillService.UpdateSkillAsync(skill);

            // Assert — values are copied onto the fetched instance
            existingSkill.Name.Should().Be("Updated Skill");
            existingSkill.Category.Should().Be("Updated Category");
            existingSkill.Proficiency.Should().Be(90);
            existingSkill.UpdatedAt.Should().NotBeNull();

            _unitOfWorkMock.Verify(u => u.Skills.GetByIdAsync(1), Times.Once);
            _validatorMock.Verify(v => v.ValidateAsync(existingSkill, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Skills.UpdateAsync(existingSkill), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            // Arrange
            var skill = new Skill { Id = 1, Name = "Updated Skill" };
            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync((Skill?)null);

            // Act
            Func<Task> act = async () => await _skillService.UpdateSkillAsync(skill);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Skill with id 1 not found.");
            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<Skill>(), default), Times.Never);
        }

        [Fact]
        public async Task DeleteSkillAsync_ShouldDeleteSkill_WhenSkillExists()
        {
            // Arrange
            var skill = new Skill { Id = 1, Name = "Skill to Delete" };
            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync(skill);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _skillService.DeleteSkillAsync(1);

            // Assert
            _unitOfWorkMock.Verify(u => u.Skills.GetByIdAsync(1), Times.Once);
            _unitOfWorkMock.Verify(u => u.Skills.DeleteAsync(skill), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSkillAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Skills.GetByIdAsync(1)).ReturnsAsync((Skill?)null);

            // Act
            Func<Task> act = async () => await _skillService.DeleteSkillAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Skill with id 1 not found.");
        }
    }
}