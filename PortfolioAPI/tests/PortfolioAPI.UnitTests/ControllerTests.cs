using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PortfolioAPI.Controllers;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces.Services;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class ControllerTests
    {
        #region ProjectsController Tests

        [Fact]
        public async Task ProjectsController_GetAllProjects_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Project 1", Description = "Description 1" },
                new Project { Id = 2, Title = "Project 2", Description = "Description 2" }
            };
            mockService.Setup(s => s.GetAllProjectsAsync()).ReturnsAsync(projects);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.GetAllProjects();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>().Subject;
            returnValue.Should().HaveCount(2);
            mockService.Verify(s => s.GetAllProjectsAsync(), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_GetProjectById_ShouldReturnOkResult_WhenProjectExists()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var project = new Project { Id = 1, Title = "Test Project", Description = "Test Description" };
            mockService.Setup(s => s.GetProjectByIdAsync(1)).ReturnsAsync(project);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.GetProjectById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Id.Should().Be(1);
            returnValue.Title.Should().Be("Test Project");
            mockService.Verify(s => s.GetProjectByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_GetFeaturedProjects_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Featured Project", IsFeatured = true }
            };
            mockService.Setup(s => s.GetFeaturedProjectsAsync()).ReturnsAsync(projects);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.GetFeaturedProjects();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<ProjectDto>>().Subject;
            returnValue.Should().ContainSingle(p => p.IsFeatured);
            mockService.Verify(s => s.GetFeaturedProjectsAsync(), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_CreateProject_ShouldReturnCreatedResult()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var project = new Project { Title = "New Project", Description = "New Description" };
            var createdProject = new Project { Id = 1, Title = "New Project", Description = "New Description" };
            mockService.Setup(s => s.CreateProjectAsync(project)).ReturnsAsync(createdProject);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.CreateProject(project);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ProjectsController.GetProjectById));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<ProjectDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(s => s.CreateProjectAsync(project), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_UpdateProject_ShouldReturnNoContent_WhenIdsMatch()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var project = new Project { Id = 1, Title = "Updated Project" };
            mockService.Setup(s => s.UpdateProjectAsync(project)).Returns(Task.CompletedTask);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.UpdateProject(1, project);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.UpdateProjectAsync(project), Times.Once);
        }

        [Fact]
        public async Task ProjectsController_UpdateProject_ShouldReturnBadRequest_WhenIdsDoNotMatch()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            var project = new Project { Id = 2, Title = "Updated Project" };
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.UpdateProject(1, project);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            mockService.Verify(s => s.UpdateProjectAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task ProjectsController_DeleteProject_ShouldReturnNoContent()
        {
            // Arrange
            var mockService = new Mock<IProjectService>();
            mockService.Setup(s => s.DeleteProjectAsync(1)).Returns(Task.CompletedTask);
            var controller = new ProjectsController(mockService.Object);

            // Act
            var result = await controller.DeleteProject(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.DeleteProjectAsync(1), Times.Once);
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
                new Skill { Id = 1, Name = "C#", Category = "Programming" },
                new Skill { Id = 2, Name = "JavaScript", Category = "Programming" }
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
            var skill = new Skill { Id = 1, Name = "C#", Category = "Programming" };
            mockService.Setup(s => s.GetSkillByIdAsync(1)).ReturnsAsync(skill);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetSkillById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<SkillDto>().Subject;
            returnValue.Id.Should().Be(1);
            returnValue.Name.Should().Be("C#");
            mockService.Verify(s => s.GetSkillByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task SkillsController_GetSkillsByCategory_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skills = new List<Skill>
            {
                new Skill { Id = 1, Name = "C#", Category = "Programming" },
                new Skill { Id = 2, Name = "SQL", Category = "Database" }
            };
            mockService.Setup(s => s.GetSkillsByCategoryAsync("Programming")).ReturnsAsync(skills.Where(s => s.Category == "Programming"));
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetSkillsByCategory("Programming");

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillDto>>().Subject;
            returnValue.Should().ContainSingle(s => s.Category == "Programming");
            mockService.Verify(s => s.GetSkillsByCategoryAsync("Programming"), Times.Once);
        }

        [Fact]
        public async Task SkillsController_GetVisibleSkills_ShouldReturnOkResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skills = new List<Skill>
            {
                new Skill { Id = 1, Name = "C#", IsVisible = true },
                new Skill { Id = 2, Name = "Hidden Skill", IsVisible = false }
            };
            mockService.Setup(s => s.GetVisibleSkillsAsync()).ReturnsAsync(skills.Where(s => s.IsVisible));
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.GetVisibleSkills();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeAssignableTo<IEnumerable<SkillDto>>().Subject;
            returnValue.Should().ContainSingle(s => s.IsVisible);
            mockService.Verify(s => s.GetVisibleSkillsAsync(), Times.Once);
        }

        [Fact]
        public async Task SkillsController_CreateSkill_ShouldReturnCreatedResult()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skill = new Skill { Name = "New Skill", Category = "New Category" };
            var createdSkill = new Skill { Id = 1, Name = "New Skill", Category = "New Category" };
            mockService.Setup(s => s.CreateSkillAsync(skill)).ReturnsAsync(createdSkill);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.CreateSkill(skill);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(SkillsController.GetSkillById));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<SkillDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(s => s.CreateSkillAsync(skill), Times.Once);
        }

        [Fact]
        public async Task SkillsController_UpdateSkill_ShouldReturnNoContent_WhenIdsMatch()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skill = new Skill { Id = 1, Name = "Updated Skill" };
            mockService.Setup(s => s.UpdateSkillAsync(skill)).Returns(Task.CompletedTask);
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.UpdateSkill(1, skill);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockService.Verify(s => s.UpdateSkillAsync(skill), Times.Once);
        }

        [Fact]
        public async Task SkillsController_UpdateSkill_ShouldReturnBadRequest_WhenIdsDoNotMatch()
        {
            // Arrange
            var mockService = new Mock<ISkillService>();
            var skill = new Skill { Id = 2, Name = "Updated Skill" };
            var controller = new SkillsController(mockService.Object);

            // Act
            var result = await controller.UpdateSkill(1, skill);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            mockService.Verify(s => s.UpdateSkillAsync(It.IsAny<Skill>()), Times.Never);
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
            // Arrange
            var mockService = new Mock<IContactService>();
            var contact = new Contact { Name = "New Contact", Email = "new@example.com", Message = "Hello" };
            var createdContact = new Contact { Id = 1, Name = "New Contact", Email = "new@example.com", Message = "Hello" };
            mockService.Setup(s => s.CreateContactAsync(contact)).ReturnsAsync(createdContact);
            var controller = new ContactController(mockService.Object);

            // Act
            var result = await controller.CreateContact(contact);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(ContactController.GetContactById));
            createdResult.RouteValues!["id"].Should().Be(1);
            var returnValue = createdResult.Value.Should().BeOfType<ContactDto>().Subject;
            returnValue.Id.Should().Be(1);
            mockService.Verify(s => s.CreateContactAsync(contact), Times.Once);
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