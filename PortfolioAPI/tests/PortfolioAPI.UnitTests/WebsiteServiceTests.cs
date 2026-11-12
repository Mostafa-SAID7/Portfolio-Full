using System.Linq.Expressions;
using FluentAssertions;
using FluentValidation;
using Moq;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;
using PortfolioAPI.Application.Services;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Interfaces;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class WebsiteServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<Website>> _websiteRepoMock;
        private readonly Mock<IRepository<ProjectWebsite>> _projectWebsiteRepoMock;
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<IValidator<Project>> _projectValidatorMock;
        private readonly WebsiteService _websiteService;

        public WebsiteServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _websiteRepoMock = new Mock<IRepository<Website>>();
            _projectWebsiteRepoMock = new Mock<IRepository<ProjectWebsite>>();
            _projectRepoMock = new Mock<IProjectRepository>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _projectValidatorMock = new Mock<IValidator<Project>>();

            _unitOfWorkMock.Setup(u => u.Websites).Returns(_websiteRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.ProjectWebsites).Returns(_projectWebsiteRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectRepoMock.Object);

            _websiteService = new WebsiteService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOrderedWebsitesWithProjectCounts()
        {
            // Arrange
            var websites = new List<Website>
            {
                new Website { Id = 2, Name = "Fintech", Slug = "fintech", Order = 2, IsActive = true },
                new Website { Id = 1, Name = "Marketplace", Slug = "marketplace", Order = 1, IsActive = true }
            };
            var projectWebsites = new List<ProjectWebsite>
            {
                new ProjectWebsite { Id = 10, ProjectId = 1, WebsiteId = 1, IsPublished = true },
                new ProjectWebsite { Id = 11, ProjectId = 2, WebsiteId = 1, IsPublished = true },
                new ProjectWebsite { Id = 12, ProjectId = 1, WebsiteId = 2, IsPublished = true }
            };
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "P1" },
                new Project { Id = 2, TitleEn = "P2" }
            };

            _websiteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(websites);
            _projectWebsiteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(projectWebsites);
            _projectRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(projects);

            // Act
            var result = await _websiteService.GetAllAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            var list = result.Data!.ToList();
            list.Count.Should().Be(2);
            list[0].Slug.Should().Be("marketplace");
            list[0].AssignedProjectsCount.Should().Be(2);
            list[1].Slug.Should().Be("fintech");
            list[1].AssignedProjectsCount.Should().Be(1);
        }

        [Fact]
        public async Task GetActiveWebsitesAsync_ShouldReturnOnlyActiveWebsites()
        {
            // Arrange
            var activeWebsites = new List<Website>
            {
                new Website { Id = 1, Name = "Marketplace", Slug = "marketplace", IsActive = true, Order = 1 }
            };
            _websiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Website, bool>>>()))
                .ReturnsAsync(activeWebsites);
            _projectWebsiteRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<ProjectWebsite>());

            // Act
            var result = await _websiteService.GetActiveWebsitesAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().Slug.Should().Be("marketplace");
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldReturnWebsiteDto()
        {
            // Arrange
            var website = new Website { Id = 1, Name = "Marketplace", Slug = "marketplace", IsActive = true };
            _websiteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(website);
            _projectWebsiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectWebsite, bool>>>()))
                .ReturnsAsync(new List<ProjectWebsite> { new ProjectWebsite { Id = 1, ProjectId = 1, WebsiteId = 1 } });
            _projectRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Project> { new Project { Id = 1 } });

            // Act
            var result = await _websiteService.GetByIdAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Slug.Should().Be("marketplace");
            result.Data.AssignedProjectsCount.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldReturnFailure()
        {
            // Arrange
            _websiteRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Website?)null);

            // Act
            var result = await _websiteService.GetByIdAsync(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task CreateAsync_WhenSlugAlreadyExists_ShouldThrowValidationException()
        {
            // Arrange
            var dto = new CreateWebsiteDto { Name = "Marketplace", Slug = "marketplace" };
            _websiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Website, bool>>>()))
                .ReturnsAsync(new List<Website> { new Website { Id = 1, Slug = "marketplace" } });

            // Act & Assert
            await FluentActions.Invoking(() => _websiteService.CreateAsync(dto))
                .Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldAddWebsiteAndSave()
        {
            // Arrange
            var dto = new CreateWebsiteDto
            {
                Name = "Real Estate",
                Slug = "real-estate",
                Description = "Property listings portal",
                BaseUrl = "https://ms-reall-estate.vercel.app",
                Order = 4,
                IsActive = true
            };
            _websiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Website, bool>>>()))
                .ReturnsAsync(new List<Website>());

            Website? addedWebsite = null;
            _websiteRepoMock.Setup(r => r.AddAsync(It.IsAny<Website>()))
                .Callback<Website>(w => addedWebsite = w)
                .ReturnsAsync((Website w) => w);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _websiteService.CreateAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Slug.Should().Be("real-estate");
            addedWebsite.Should().NotBeNull();
            addedWebsite!.Name.Should().Be("Real Estate");
            _websiteRepoMock.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenWebsiteExists_ShouldUpdateFields()
        {
            // Arrange
            var existing = new Website { Id = 1, Name = "Old Name", Slug = "old-slug", Order = 1, IsActive = true };
            var updateDto = new UpdateWebsiteDto { Name = "New Name", Slug = "new-slug", Order = 5, IsActive = false };

            _websiteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _websiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Website, bool>>>()))
                .ReturnsAsync(new List<Website>());
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _websiteService.UpdateAsync(1, updateDto);

            // Assert
            result.Success.Should().BeTrue();
            existing.Name.Should().Be("New Name");
            existing.Slug.Should().Be("new-slug");
            existing.Order.Should().Be(5);
            existing.IsActive.Should().BeFalse();
            _websiteRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenFound_ShouldSoftDelete()
        {
            // Arrange
            var existing = new Website { Id = 1, Name = "To Delete", Slug = "to-delete" };

            _websiteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _websiteService.DeleteAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            _websiteRepoMock.Verify(r => r.DeleteAsync(existing), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ToggleActiveAsync_ShouldFlipActiveState()
        {
            // Arrange
            var existing = new Website { Id = 1, Name = "Marketplace", Slug = "marketplace", IsActive = true };
            _websiteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _websiteService.ToggleActiveAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeFalse();
            existing.IsActive.Should().BeFalse();
            _websiteRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task ProjectService_UpdateWebsiteAssignmentsAsync_ShouldSyncCorrectly()
        {
            // Arrange
            var projectService = new ProjectService(
                _unitOfWorkMock.Object,
                _projectValidatorMock.Object,
                _categoryServiceMock.Object);

            var project = new Project { Id = 5, TitleEn = "Test Project" };
            var existingAssignments = new List<ProjectWebsite>
            {
                new ProjectWebsite { Id = 1, ProjectId = 5, WebsiteId = 1, IsPublished = true, DisplayOrder = 0 }, // keep and update
                new ProjectWebsite { Id = 2, ProjectId = 5, WebsiteId = 2, IsPublished = true, DisplayOrder = 0 }  // remove
            };

            var websites = new List<Website>
            {
                new Website { Id = 1, Slug = "marketplace", IsActive = true },
                new Website { Id = 3, Slug = "edutech", IsActive = true }
            };

            _projectRepoMock.Setup(p => p.GetByIdAsync(5)).ReturnsAsync(project);
            _projectWebsiteRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectWebsite, bool>>>()))
                .ReturnsAsync(existingAssignments);
            _websiteRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(websites);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var newAssignments = new List<AssignProjectWebsiteDto>
            {
                new AssignProjectWebsiteDto { WebsiteId = 1, IsPublished = false, DisplayOrder = 2 },
                new AssignProjectWebsiteDto { WebsiteId = 3, IsPublished = true, DisplayOrder = 1 }
            };

            // Act
            var result = await projectService.UpdateWebsiteAssignmentsAsync(5, new UpdateProjectWebsitesDto { Websites = newAssignments });

            // Assert
            result.Success.Should().BeTrue();
            // Verify removal of WebsiteId 2
            _projectWebsiteRepoMock.Verify(r => r.DeleteAsync(It.Is<ProjectWebsite>(pw => pw.WebsiteId == 2)), Times.Once);
            // Verify update of WebsiteId 1
            _projectWebsiteRepoMock.Verify(r => r.UpdateAsync(It.Is<ProjectWebsite>(pw => pw.WebsiteId == 1 && !pw.IsPublished && pw.DisplayOrder == 2)), Times.Once);
            // Verify addition of WebsiteId 3
            _projectWebsiteRepoMock.Verify(r => r.AddAsync(It.Is<ProjectWebsite>(pw => pw.WebsiteId == 3 && pw.IsPublished && pw.DisplayOrder == 1)), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
