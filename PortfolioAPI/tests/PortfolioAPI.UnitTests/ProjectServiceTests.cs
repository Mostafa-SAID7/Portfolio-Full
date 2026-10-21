using System.Linq.Expressions;
using FluentAssertions;
using FluentValidation;
using Moq;
using PortfolioAPI.Application.Services;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using Xunit;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.UnitTests
{
    public class ProjectServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<Project>> _validatorMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<Project>>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _projectService = new ProjectService(
                _unitOfWorkMock.Object,
                _validatorMock.Object,
                _categoryServiceMock.Object);
        }

        private void ValidatorSucceeds()
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOrderedProjectsAsDtos()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Second", TitleAr = "ثاني", DescriptionEn = "D2", DescriptionAr = "د2", Order = 2 },
                new Project { Id = 2, TitleEn = "First", TitleAr = "أول", DescriptionEn = "D1", DescriptionAr = "د1", Order = 1 }
            };
            _unitOfWorkMock.Setup(u => u.Projects.GetAllAsync()).ReturnsAsync(projects);

            // Act
            var result = await _projectService.GetAllAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Select(p => p.TitleEn).Should().Equal("First", "Second");
            _unitOfWorkMock.Verify(u => u.Projects.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProjectDto_WhenProjectExists()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Test Project", TitleAr = "مشروع اختبار", DescriptionEn = "Test Description", DescriptionAr = "وصف الاختبار" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(project);

            // Act
            var result = await _projectService.GetByIdAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(1);
            result.Data.TitleEn.Should().Be("Test Project");
            result.Data.DescriptionEn.Should().Be("Test Description");
            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotFoundResult_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            var result = await _projectService.GetByIdAsync(1);

            // Assert
            result.Success.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Project with id 1 was not found.");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetFeaturedAsync_ShouldReturnFeaturedActiveProjects_OrderedByOrder()
        {
            // Arrange
            var featured = new Project { Id = 1, TitleEn = "Featured", TitleAr = "مميز", DescriptionEn = "D", DescriptionAr = "د", Featured = true, IsActive = true, Order = 1 };
            var projects = new List<Project>
            {
                new Project { Id = 2, TitleEn = "Hidden", TitleAr = "مخفي", DescriptionEn = "D", DescriptionAr = "د", Featured = false, IsActive = true },
                new Project { Id = 3, TitleEn = "Inactive", TitleAr = "غير نشط", DescriptionEn = "D", DescriptionAr = "د", Featured = true, IsActive = false },
                featured
            };

            // The service filters with p => p.Featured && p.IsActive; capture the
            // predicate to assert it selects correctly, and simulate the repo by
            // returning only the rows the predicate matches.
            Expression<Func<Project, bool>>? captured = null;
            _unitOfWorkMock
                .Setup(u => u.Projects.FindAsync(It.IsAny<Expression<Func<Project, bool>>>()))
                .Callback((Expression<Func<Project, bool>> expr) => captured = expr)
                .ReturnsAsync(projects.Where(p => p.Featured && p.IsActive));

            // Act
            var result = await _projectService.GetFeaturedAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle();
            var featuredDto = result.Data!.Single();
            featuredDto.Id.Should().Be(1);
            featuredDto.Featured.Should().BeTrue();

            captured.Should().NotBeNull();
            captured!.Compile()(featured).Should().BeTrue();
            captured.Compile()(projects[0]).Should().BeFalse();
            captured.Compile()(projects[1]).Should().BeFalse();
            _unitOfWorkMock.Verify(
                u => u.Projects.FindAsync(It.IsAny<Expression<Func<Project, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task GetByTechnologyAsync_ShouldReturnMatchingProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "P1", TitleAr = "ب1", DescriptionEn = "D", DescriptionAr = "د", Technologies = new List<string> { "C#" } }
            };
            _unitOfWorkMock.Setup(u => u.Projects.GetByTechnologyAsync("C#")).ReturnsAsync(projects);

            // Act
            var result = await _projectService.GetByTechnologyAsync("C#");

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(p => p.TitleEn == "P1");
            result.Data!.Single().Technologies.Should().Contain("C#");
            _unitOfWorkMock.Verify(u => u.Projects.GetByTechnologyAsync("C#"), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnMatchingProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Portfolio API", TitleAr = "بورتفوليو API", DescriptionEn = "Search hit", DescriptionAr = "نتيجة بحث" }
            };
            _unitOfWorkMock.Setup(u => u.Projects.SearchAsync("portfolio")).ReturnsAsync(projects);

            // Act
            var result = await _projectService.SearchAsync("portfolio");

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().ContainSingle(p => p.TitleEn == "Portfolio API");
            _unitOfWorkMock.Verify(u => u.Projects.SearchAsync("portfolio"), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateProject_WhenValidationPasses()
        {
            // Arrange
            ValidatorSucceeds();
            _unitOfWorkMock
                .Setup(u => u.Projects.AddAsync(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => { p.Id = 1; return p; });
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var dto = new CreateProjectDto
            {
                TitleEn = "  New Project  ",
                TitleAr = "  مشروع جديد  ",
                DescriptionEn = "  New Description  ",
                DescriptionAr = "  وصف جديد  ",
                Technologies = new List<string> { "  C#  ", "", "  React  " },
                CategoryEn = "  Web  ",
                CategoryAr = "  ويب  "
            };

            // Act
            var result = await _projectService.CreateAsync(dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Project created.");
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(1);
            result.Data.TitleEn.Should().Be("New Project");
            result.Data.DescriptionEn.Should().Be("New Description");
            result.Data.Technologies.Should().Equal("C#", "React");
            result.Data.CategoryEn.Should().Be("Web");

            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.AddAsync(It.IsAny<Project>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Title", "Title is required.")
            };
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(failures));

            var dto = new CreateProjectDto { TitleEn = "", TitleAr = "", DescriptionEn = "", DescriptionAr = "" };

            // Act
            Func<Task> act = async () => await _projectService.CreateAsync(dto);

            // Assert
            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Should().ContainKey("Title")
                .WhoseValue.Should().Contain("Title is required.");
            _unitOfWorkMock.Verify(u => u.Projects.AddAsync(It.IsAny<Project>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateProject_WhenProjectExistsAndValidationPasses()
        {
            // Arrange
            var existingProject = new Project { Id = 1, TitleEn = "Old Project", TitleAr = "مشروع قديم", DescriptionEn = "Old Description", DescriptionAr = "وصف قديم" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);
            _unitOfWorkMock.Setup(u => u.Projects.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            ValidatorSucceeds();

            var dto = new UpdateProjectDto
            {
                TitleEn = "  Updated Project  ",
                TitleAr = "  مشروع محدث  ",
                DescriptionEn = "  Updated Description  ",
                DescriptionAr = "  وصف محدث  ",
                Technologies = new List<string> { " .NET " },
                CategoryEn = "  APIs  ",
                CategoryAr = "  واجهات  "
            };

            // Act
            var result = await _projectService.UpdateAsync(1, dto);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Project updated.");
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(1);
            result.Data.TitleEn.Should().Be("Updated Project");
            result.Data.Technologies.Should().Equal(".NET");
            result.Data.CategoryEn.Should().Be("APIs");

            existingProject.TitleEn.Should().Be("Updated Project");
            existingProject.UpdatedAt.Should().NotBeNull();

            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(existingProject), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenThePayloadOmitsStorageMetadata_KeepsTheStoredProviderIdentity()
        {
            // Regressions guard: an older client (or a manual API call) PUTs the
            // project without the storage fields. The unchanged cover must not
            // lose its provider/public id — that would make the asset
            // unmanageable and could trigger a spurious delete.
            var existingProject = new Project
            {
                Id = 1,
                TitleEn = "Haxel",
                TitleAr = "هاكسل",
                DescriptionEn = "D",
                DescriptionAr = "و",
                ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/portfolio/projects/haxel/cover.png",
                StorageProvider = MediaProvider.Cloudinary,
                StoragePublicId = "portfolio/projects/haxel/cover"
            };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);
            _unitOfWorkMock.Setup(u => u.Projects.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            ValidatorSucceeds();

            var dto = new UpdateProjectDto
            {
                TitleEn = "Haxel",
                TitleAr = "هاكسل",
                DescriptionEn = "D",
                DescriptionAr = "و",
                // Same cover as stored, but a client predating the storage
                // columns sends no provider/public id with it.
                ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1/portfolio/projects/haxel/cover.png"
            };

            var result = await _projectService.UpdateAsync(1, dto);

            result.Success.Should().BeTrue();
            existingProject.StorageProvider.Should().Be(MediaProvider.Cloudinary);
            existingProject.StoragePublicId.Should().Be("portfolio/projects/haxel/cover");
        }

        [Fact]
        public async Task UpdateAsync_WhenThePayloadCarriesStorageMetadata_AppliesIt()
        {
            var existingProject = new Project { Id = 1, TitleEn = "Haxel", TitleAr = "هاكسل", DescriptionEn = "D", DescriptionAr = "و" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);
            _unitOfWorkMock.Setup(u => u.Projects.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            ValidatorSucceeds();

            var dto = new UpdateProjectDto
            {
                TitleEn = "Haxel",
                TitleAr = "هاكسل",
                DescriptionEn = "D",
                DescriptionAr = "و",
                ImageUrl = "https://res.cloudinary.com/demo/image/upload/v2/portfolio/projects/haxel/cover.png",
                StorageProvider = MediaProvider.Cloudinary,
                StoragePublicId = "portfolio/projects/haxel/cover"
            };

            var result = await _projectService.UpdateAsync(1, dto);

            result.Success.Should().BeTrue();
            existingProject.StorageProvider.Should().Be(MediaProvider.Cloudinary);
            existingProject.StoragePublicId.Should().Be("portfolio/projects/haxel/cover");
        }

        [Fact]
        public async Task UpdateAsync_WhenTheCoverIsCleared_ClearsTheStoredProviderIdentity()
        {
            var existingProject = new Project
            {
                Id = 1,
                TitleEn = "Haxel",
                TitleAr = "هاكسل",
                DescriptionEn = "D",
                DescriptionAr = "و",
                ImageUrl = "/api/files/haxel-footwear-1280.webp",
                StorageProvider = MediaProvider.Local,
                StoragePublicId = "haxel-footwear-1280.webp"
            };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);
            _unitOfWorkMock.Setup(u => u.Projects.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            ValidatorSucceeds();

            var dto = new UpdateProjectDto
            {
                TitleEn = "Haxel",
                TitleAr = "هاكسل",
                DescriptionEn = "D",
                DescriptionAr = "و",
                ImageUrl = "" // admin removed the cover in the dashboard
            };

            var result = await _projectService.UpdateAsync(1, dto);

            result.Success.Should().BeTrue();
            existingProject.ImageUrl.Should().BeEmpty();
            existingProject.StorageProvider.Should().BeNull();
            existingProject.StoragePublicId.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNotFoundResult_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            var result = await _projectService.UpdateAsync(1, new UpdateProjectDto { TitleEn = "X", TitleAr = "م", DescriptionEn = "Y", DescriptionAr = "و" });

            // Assert
            result.Success.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Project with id 1 was not found.");
            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(It.IsAny<Project>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var existingProject = new Project { Id = 1, TitleEn = "Old", TitleAr = "قديم", DescriptionEn = "Old", DescriptionAr = "قديم" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(existingProject);

            var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Title", "Title is required.")
            };
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(failures));

            // Act
            Func<Task> act = async () => await _projectService.UpdateAsync(
                1, new UpdateProjectDto { TitleEn = "", TitleAr = "", DescriptionEn = "Desc", DescriptionAr = "وصف" });

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(It.IsAny<Project>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteProject_WhenProjectExists()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Project to Delete", TitleAr = "مشروع للحذف", DescriptionEn = "D", DescriptionAr = "وصف" };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(project);
            _unitOfWorkMock.Setup(u => u.Projects.DeleteAsync(project)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _projectService.DeleteAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Project deleted.");
            _unitOfWorkMock.Verify(u => u.Projects.GetByIdAsync(1), Times.Once);
            _unitOfWorkMock.Verify(u => u.Projects.DeleteAsync(project), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnNotFoundResult_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            var result = await _projectService.DeleteAsync(1);

            // Assert
            result.Success.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Be("Project with id 1 was not found.");
            _unitOfWorkMock.Verify(u => u.Projects.DeleteAsync(It.IsAny<Project>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ToggleFeaturedAsync_ShouldFlipFeaturedFlag_WhenProjectExists()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "P", TitleAr = "ب", DescriptionEn = "D", DescriptionAr = "وصف", Featured = false };
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync(project);
            _unitOfWorkMock.Setup(u => u.Projects.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _projectService.ToggleFeaturedAsync(1);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            project.Featured.Should().BeTrue();
            project.UpdatedAt.Should().NotBeNull();
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(project), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ToggleFeaturedAsync_ShouldReturnNotFoundResult_WhenProjectDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1)).ReturnsAsync((Project?)null);

            // Act
            var result = await _projectService.ToggleFeaturedAsync(1);

            // Assert
            result.Success.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.Projects.UpdateAsync(It.IsAny<Project>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}
