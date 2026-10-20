using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Controllers;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;
using PortfolioAPI.Infrastructure.Services;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class ControllerTests
    {
        #region ProjectsController Tests

        private static ProjectsController CreateProjectsController(
            Mock<IProjectService> mockService,
            Mock<IFileStorageService>? mockFileStorage = null,
            Mock<IConfiguration>? mockConfiguration = null)
        {
            return new ProjectsController(
                mockService.Object,
                (mockFileStorage ?? new Mock<IFileStorageService>()).Object,
                (mockConfiguration ?? new Mock<IConfiguration>()).Object,
                NullLogger<ProjectsController>.Instance);
        }

        [Fact]
        public async Task ProjectsController_GetAllProjects_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var projects = new List<ProjectDto>
            {
                new ProjectDto { Id = 1, TitleEn = "Project 1", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 1", DescriptionEn = "Description 1", DescriptionAr = "ÙˆØµÙ 1" },
                new ProjectDto { Id = 2, TitleEn = "Project 2", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 2", DescriptionEn = "Description 2", DescriptionAr = "ÙˆØµÙ 2" }
            };
            mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(Result<IEnumerable<ProjectDto>>.Ok(projects));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.GetAllProjects();

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var returnValue = objectResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>().Subject;
            returnValue.Should().HaveCount(2);
            mockService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_GetProject_ShouldReturnOkResult_WhenProjectExists()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var project = new ProjectDto { Id = 1, TitleEn = "Test Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ø§Ø®ØªØ¨Ø§Ø±", DescriptionEn = "Test Description", DescriptionAr = "ÙˆØµÙ Ø§Ù„Ø§Ø®ØªØ¨Ø§Ø±" };
            mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(Result<ProjectDto>.Ok(project));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.GetProject(1);

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var returnValue = objectResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Id.Should().Be(1);
            returnValue.TitleEn.Should().Be("Test Project");
            mockService.Verify(s => s.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_GetProject_ShouldReturn404_WhenProjectDoesNotExist()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            mockService.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync(Result<ProjectDto>.NotFound("Project with id 99 was not found."));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.GetProject(99);

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            objectResult.Value.Should().NotBeNull();
            mockService.Verify(s => s.GetByIdAsync(99), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_GetFeaturedProjects_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var projects = new List<ProjectDto>
            {
                new ProjectDto { Id = 1, TitleEn = "Featured Project", TitleAr = "مشروع مميز", Featured = true }
            };
            mockService.Setup(s => s.GetFeaturedAsync())
                .ReturnsAsync(Result<IEnumerable<ProjectDto>>.Ok(projects));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.GetFeaturedProjects();

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var returnValue = objectResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>().Subject;
            returnValue.Should().ContainSingle(p => p.Featured);
            mockService.Verify(s => s.GetFeaturedAsync(), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_SearchProjects_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var projects = new List<ProjectDto>
            {
                new ProjectDto { Id = 1, TitleEn = "Portfolio API", TitleAr = "بورتفوليو API" }
            };
            mockService.Setup(s => s.SearchAsync("portfolio"))
                .ReturnsAsync(Result<IEnumerable<ProjectDto>>.Ok(projects));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.SearchProjects("portfolio");

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            objectResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>();
            mockService.Verify(s => s.SearchAsync("portfolio"), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_CreateProject_ShouldReturnCreatedResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var dto = new CreateProjectDto { Title = "New Project", Description = "New Description" };
            var createdProject = new ProjectDto { Id = 1, Title = "New Project", Description = "New Description" };
            mockService.Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(Result<ProjectDto>.Ok(createdProject, "Project created."));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.CreateProject(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ProjectsController.GetProject));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_CreateProject_ShouldPropagateValidationException_WhenValidationFails()
        {
            // Arrange â€” the real contract: ProjectService.CreateAsync validates
            // the assembled entity and THROWS ValidationException (see
            // ProjectService.cs); the exception middleware in Program.cs renders
            // the shared 400 { message, errors } body. The controller itself
            // must let it propagate, not swallow it into a 200/500.
            var mockService = new Mock<IProjectService>();
            var dto = new CreateProjectDto { Title = "", Description = "" };
            var validationException = new ValidationException(new[]
            {
                new ValidationFailure("Title", "Title is required.")
            });
            mockService.Setup(s => s.CreateAsync(dto)).ThrowsAsync(validationException);
            var controller = CreateProjectsController(mockService);

            // Act
            var act = () => controller.CreateProject(dto);

            // Assert
            var assertions = await act.Should().ThrowAsync<ValidationException>();
            var exception = assertions.Which;
            exception.Errors.Should().ContainKey("Title");
            exception.Errors["Title"].Should().Contain("Title is required.");
        }

        [Fact]
        public async Task ProjectsController_UpdateProject_ShouldReturnNoContent_WhenUpdateSucceeds()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var dto = new UpdateProjectDto { Title = "Updated Project", Description = "Updated" };
            var existing = new ProjectDto { Id = 1, Title = "Old Project" };
            mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(Result<ProjectDto>.Ok(existing));
            mockService.Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(Result<ProjectDto>.Ok(existing, "Project updated."));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.UpdateProject(1, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.UpdateAsync(1, dto), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_UpdateProject_ShouldReturn404_WhenProjectDoesNotExist()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var dto = new UpdateProjectDto { Title = "Updated Project", Description = "Updated" };
            mockService.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync(Result<ProjectDto>.NotFound("Project with id 99 was not found."));
            mockService.Setup(s => s.UpdateAsync(99, dto))
                .ReturnsAsync(Result<ProjectDto>.NotFound("Project with id 99 was not found."));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.UpdateProject(99, dto);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ProjectsController_DeleteProject_ShouldReturnNoContent()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var existing = new ProjectDto { Id = 1, Title = "Project to Delete" };
            mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(Result<ProjectDto>.Ok(existing));
            mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(Result<bool>.Ok(true, "Project deleted."));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.DeleteProject(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_ToggleFeatured_ShouldReturnOkWithFlag()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            mockService.Setup(s => s.ToggleFeaturedAsync(1))
                .ReturnsAsync(Result<bool>.Ok(true));
            var controller = CreateProjectsController(mockService);

            // Act
            var result = await controller.ToggleFeatured(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            mockService.Verify(s => s.ToggleFeaturedAsync(1), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_DeleteProject_ShouldDeleteStoredCover_WhenCoverChanged()
        {
            // Arrange â€” the stored cover is under /api/files (default base URL)
            var mockService = new Mock<IProjectService>();
            var mockFileStorage = new Mock<IFileStorageService>();
            var dto = new UpdateProjectDto { Title = "Updated", Description = "Updated", ImageUrl = null };
            var existing = new ProjectDto { Id = 1, Title = "Old", ImageUrl = "/api/files/old-cover.png" };
            mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(Result<ProjectDto>.Ok(existing));
            mockService.Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(Result<ProjectDto>.Ok(existing, "Project updated."));
            var controller = CreateProjectsController(mockService, mockFileStorage);

            // Act
            var result = await controller.UpdateProject(1, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockFileStorage.Verify(f => f.DeleteFileAsync("/api/files/old-cover.png"), Times.Once);
        }

        #endregion

        #region SkillsController Tests

        [Fact]
        public async Task SkillsController_GetAllSkills_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skills = new List<Skill>
            {
                new Skill { Id = 1, NameEn = "C#", NameAr = "Ø³ÙŠ Ø´Ø§Ø±Ø¨", CategoryEn = "Programming", CategoryAr = "Ø¨Ø±Ù…Ø¬Ø©" },
                new Skill { Id = 2, NameEn = "JavaScript", NameAr = "Ø¬Ø§ÙØ§ Ø³ÙƒØ±ÙŠØ¨Øª", CategoryEn = "Programming", CategoryAr = "Ø¨Ø±Ù…Ø¬Ø©" }
            };
            mockService.Setup(s => s.GetAllSkillsAsync()).ReturnsAsync(skills);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetAllSkills();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillDto>>().Subject;
            returnValue.Should().HaveCount(2);
            mockService.Verify(s => s.GetAllSkillsAsync(), Times.Once);
        }

        [Fact]
        public async Task SkillsController_GetSkillById_ShouldReturnOkResult_WhenSkillExists()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skill = new Skill { Id = 1, NameEn = "C#", NameAr = "Ø³ÙŠ Ø´Ø§Ø±Ø¨", CategoryEn = "Programming", CategoryAr = "Ø¨Ø±Ù…Ø¬Ø©", Proficiency = 90 };
            mockService.Setup(s => s.GetSkillByIdAsync(1)).ReturnsAsync(skill);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetSkillById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<SkillDto>().Subject;
            returnValue.Id.Should().Be(1);
            returnValue.NameEn.Should().Be("C#");
            returnValue.Proficiency.Should().Be(90);
            mockService.Verify(s => s.GetSkillByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task SkillsController_GetVisibleSkills_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skills = new List<Skill>
            {
                new Skill { Id = 1, NameEn = "C#", NameAr = "Ø³ÙŠ Ø´Ø§Ø±Ø¨", IsActive = true },
                new Skill { Id = 2, NameEn = "Hidden Skill", NameAr = "Ù…Ù‡Ø§Ø±Ø© Ù…Ø®ÙÙŠØ©", IsActive = false }
            };
            mockService.Setup(s => s.GetVisibleSkillsAsync()).ReturnsAsync(skills.Where(s => s.IsActive));
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetVisibleSkills();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillDto>>().Subject;
            returnValue.Should().ContainSingle(s => s.IsActive);
            mockService.Verify(s => s.GetVisibleSkillsAsync(), Times.Once);
        }

        [Fact]
        public async Task SkillsController_CreateSkill_ShouldReturnCreatedResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var dto = new CreateSkillDto { NameEn = "New Skill", NameAr = "Ù…Ù‡Ø§Ø±Ø© Ø¬Ø¯ÙŠØ¯Ø©", CategoryEn = "New Category", CategoryAr = "ÙØ¦Ø© Ø¬Ø¯ÙŠØ¯Ø©", Proficiency = 75 };
            var createdSkill = new Skill { Id = 1, NameEn = "New Skill", NameAr = "Ù…Ù‡Ø§Ø±Ø© Ø¬Ø¯ÙŠØ¯Ø©", CategoryEn = "New Category", CategoryAr = "ÙØ¦Ø© Ø¬Ø¯ÙŠØ¯Ø©", Proficiency = 75 };
            mockService.Setup(s => s.CreateSkillAsync(It.IsAny<Skill>())).ReturnsAsync(createdSkill);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.CreateSkill(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(SkillsController.GetSkillById));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<SkillDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(
                s => s.CreateSkillAsync(It.Is<Skill>(sk => sk.NameEn == "New Skill" && sk.CategoryEn == "New Category")),
                Times.Once);
        }

        [Fact]
        public async Task SkillsController_UpdateSkill_ShouldReturnNoContent_UsingRouteId()
        {
            // Arrange â€” route id wins; the DTO carries no id
            var mockService = new Mock<ISkillService>();
            var dto = new UpdateSkillDto { NameEn = "Updated Skill", NameAr = "Ù…Ù‡Ø§Ø±Ø© Ù…Ø­Ø¯Ø«Ø©", CategoryEn = "Updated Category", CategoryAr = "ÙØ¦Ø© Ù…Ø­Ø¯Ø«Ø©" };
            mockService.Setup(s => s.UpdateSkillAsync(It.IsAny<Skill>())).Returns(Task.CompletedTask);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.UpdateSkill(5, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(
                s => s.UpdateSkillAsync(It.Is<Skill>(sk => sk.Id == 5 && sk.NameEn == "Updated Skill")),
                Times.Once);
        }

        [Fact]
        public async Task SkillsController_DeleteSkill_ShouldReturnNoContent()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            mockService.Setup(s => s.DeleteSkillAsync(1)).Returns(Task.CompletedTask);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.DeleteSkill(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.DeleteSkillAsync(1), Times.Once);
        }

        #endregion

        #region ContactController Tests

        [Fact]
        public async Task ContactController_GetAllContacts_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IContactService>();
            var contacts = new List<Contact>
            {
                new Contact { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new Contact { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
            };
            mockService.Setup(s => s.GetAllContactsAsync()).ReturnsAsync(contacts);
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.GetAllContacts();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<ContactDto>>().Subject;
            returnValue.Should().HaveCount(2);
            mockService.Verify(s => s.GetAllContactsAsync(), Times.Once);
        }

        [Fact]
        public async Task ContactController_GetContactById_ShouldReturnOkResult_WhenContactExists()
        {
            // Arrange
            var mockService = new Mock<IContactService>();
            var contact = new Contact { Id = 1, Name = "John Doe", Email = "john@example.com" };
            mockService.Setup(s => s.GetContactByIdAsync(1)).ReturnsAsync(contact);
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.GetContactById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<ContactDto>().Subject;
            returnValue.Id.Should().Be(1);
            returnValue.Name.Should().Be("John Doe");
            mockService.Verify(s => s.GetContactByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task ContactController_GetUnreadContacts_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IContactService>();
            var contacts = new List<Contact>
            {
                new Contact { Id = 1, Name = "John", IsRead = false },
                new Contact { Id = 2, Name = "Jane", IsRead = true }
            };
            mockService.Setup(s => s.GetUnreadContactsAsync()).ReturnsAsync(contacts.Where(c => !c.IsRead));
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.GetUnreadContacts();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<ContactDto>>().Subject;
            returnValue.Should().ContainSingle(c => !c.IsRead);
            mockService.Verify(s => s.GetUnreadContactsAsync(), Times.Once);
        }

        [Fact]
        public async Task ContactController_CreateContact_ShouldReturnCreatedResult()
        {
            // Arrange â€” the action reads HttpContext (IP/user-agent), so the
            // controller needs a real (default) HttpContext.
            var mockService = new Mock<IContactService>();
            var dto = new CreateContactDto
            {
                Name = "New Contact",
                Email = "new@example.com",
                Subject = "Hello",
                Message = "Hello there"
            };
            var createdContact = new Contact
            {
                Id = 1, Name = "New Contact", Email = "new@example.com", Subject = "Hello", Message = "Hello there"
            };
            mockService.Setup(s => s.CreateContactAsync(It.IsAny<Contact>())).ReturnsAsync(createdContact);
            var controller = new ContactController(mockService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            // Act
            var result = await controller.CreateContact(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ContactController.GetContactById));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<ContactDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(
                s => s.CreateContactAsync(It.Is<Contact>(c =>
                    c.Name == "New Contact" &&
                    c.Email == "new@example.com" &&
                    c.Subject == "Hello" &&
                    c.Message == "Hello there")),
                Times.Once);
        }

        [Fact]
        public async Task ContactController_MarkAsRead_ShouldReturnNoContent()
        {
            // Arrange
            var mockService = new Mock<IContactService>();
            mockService.Setup(s => s.MarkAsReadAsync(1)).Returns(Task.CompletedTask);
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.MarkAsRead(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.MarkAsReadAsync(1), Times.Once);
        }

        [Fact]
        public async Task ContactController_DeleteContact_ShouldReturnNoContent()
        {
            // Arrange
            var mockService = new Mock<IContactService>();
            mockService.Setup(s => s.DeleteContactAsync(1)).Returns(Task.CompletedTask);
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.DeleteContact(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.DeleteContactAsync(1), Times.Once);
        }

        #endregion
    }
}
