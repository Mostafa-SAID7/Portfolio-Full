using FluentAssertions;
using Moq;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Services;
using Xunit;
using FluentValidation;

namespace PortfolioAPI.UnitTests
{
    public class ProjectServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<Project>> _validatorMock;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<Project>>();
            _projectService = new ProjectService(_unitOfWorkMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task GetAllProjectsAsync_ShouldReturnAllProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Project 1", Description = "Description 1" },
                new Project { Id = 2, Title = "Project 2", Description = "Description 2" }
            };
            _unitOfWorkMock.Setup(u => u.Projects.GetAllAsync()).ReturnsAsync(projects);

            // Act
            var result = await _projectService.GetAllProjectsAsync();

            // Assert
            result.Should().BeEquivalentTo(projects);
            _unitOfWorkMock.Verify(u => u.Projects.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetProjectByIdAsync_ShouldReturnProject_WhenProjectExists()
        {
            // Arrange
            var project = new Project { Id = 1, Title = "Test Project", Description = "Test Description" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(project);

            // Act
            var result = await _projectService.GetProjectByIdAsync(1);

            // Assert
            result.Should().Be(project);
            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetProjectByIdAsync_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = async () => await _projectService.GetProjectByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Project with id 1 not found.");
        }

        [Fact]
        public async Task GetFeaturedProjectsAsync_ShouldReturnFeaturedProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Featured Project", IsFeatured = true },
                new Project { Id = 2, Title = "Regular Project", IsFeatured = false }
            };
            _unitOfWorkMock.Setup(u => u.Projects.FindAsync(p => p.IsFeatured)).ReturnsAsync(projects.Where(p => p.IsFeatured));

            // Act
            var result = await _projectService.GetFeaturedProjectsAsync();

            // Assert
            result.Should().ContainSingle(p => p.IsFeatured);
            _unitOfWorkMock.Verify(u => u.Projects.FindAsync(p => p.IsFeatured), Times.Once);
        }

        [Fact]
        public async Task CreateProjectAsync_ShouldCreateProject_WhenValidationPasses()
        {
            // Arrange
            var project = new Project { Title = "New Project", Description = "New Description" };
            var validationResult = new FluentValidation.Results.ValidationResult();
            var createdProject = new Project { Id = 1, Title = "New Project", Description = "New Description", CreatedAt = DateTime.UtcNow };

            _validatorMock.Setup(v => v.ValidateAsync(project, default)).ReturnsAsync(validationResult);
            _unitOfWorkMock.Setup(u => u.Projects.AddAsync(It.IsAny<Project>())).ReturnsAsync(createdProject);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _projectService.CreateProjectAsync(project);

            // Assert
            result.Should().Be(createdProject);
            _validatorMock.Verify(v => v.ValidateAsync(project, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.AddAsync(It.IsAny<Project>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateProjectAsync_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var project = new Project { Title = "", Description = "" };
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Title", "Title is required.")
            };
            var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);

            _validatorMock.Setup(v => v.ValidateAsync(project, default)).ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _projectService.CreateProjectAsync(project);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _validatorMock.Verify(v => v.ValidateAsync(project, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.AddAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProjectAsync_ShouldUpdateProject_WhenProjectExistsAndValidationPasses()
        {
            // Arrange
            var project = new Project { Id = 1, Title = "Updated Project", Description = "Updated Description" };
            var existingProject = new Project { Id = 1, Title = "Old Project", Description = "Old Description" };
            var validationResult = new FluentValidation.Results.ValidationResult();

            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);
            _validatorMock.Setup(v => v.ValidateAsync(project, default)).ReturnsAsync(validationResult);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _projectService.UpdateProjectAsync(project);

            // Assert
            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
            _validatorMock.Verify(v => v.ValidateAsync(project, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(project), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProjectAsync_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            var project = new Project { Id = 1, Title = "Updated Project" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = async () => await _projectService.UpdateProjectAsync(project);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Project with id 1 not found.");
            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<Project>(), default), Times.Never);
        }

        [Fact]
        public async Task DeleteProjectAsync_ShouldDeleteProject_WhenProjectExists()
        {
            // Arrange
            var project = new Project { Id = 1, Title = "Project to Delete" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(project);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _projectService.DeleteProjectAsync(1);

            // Assert
            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.DeleteAsync(project), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteProjectAsync_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            Func<Task> act = async () => await _projectService.DeleteProjectAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Project with id 1 not found.");
        }
    }
}