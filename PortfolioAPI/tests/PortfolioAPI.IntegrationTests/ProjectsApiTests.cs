using System.Net;
using Bogus;
using FluentAssertions;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class ProjectsApiTests : TestBase
    {
        private readonly Faker<Project> _projectFaker;

        public ProjectsApiTests()
        {
            _projectFaker = new Faker<Project>()
                .RuleFor(p => p.Title, f => f.Lorem.Sentence(3))
                .RuleFor(p => p.Description, f => f.Lorem.Paragraphs(2))
                .RuleFor(p => p.Technologies, f => f.Make(3, () => f.Hacker.Noun()))
                .RuleFor(p => p.LiveUrl, f => f.Internet.Url())
                .RuleFor(p => p.GithubUrl, f => f.Internet.Url())
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.Featured, f => f.Random.Bool());
        }

        [Fact]
        public async Task GetAllProjects_ReturnsEmptyList_WhenNoProjectsExist()
        {
            // Act
            var response = await GetAsync("api/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projects = await DeserializeResponseAsync<IEnumerable<ProjectDto>>(response);
            projects.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllProjects_ReturnsAllProjects_WhenProjectsExist()
        {
            // Arrange
            var testProjects = _projectFaker.Generate(3);
            await _dbContext.Projects.AddRangeAsync(testProjects);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projects = await DeserializeResponseAsync<IEnumerable<ProjectDto>>(response);
            projects.Should().HaveCount(3);
            projects.Should().AllSatisfy(p =>
            {
                p.Id.Should().BeGreaterThan(0);
                p.Title.Should().NotBeNullOrEmpty();
                p.Description.Should().NotBeNullOrEmpty();
                p.CreatedAt.Should().NotBe(default);
                p.UpdatedAt.Should().NotBe(default);
            });
        }

        [Fact]
        public async Task GetProjectById_ReturnsProject_WhenProjectExists()
        {
            // Arrange
            var testProject = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(testProject);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync($"api/projects/{testProject.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var project = await DeserializeResponseAsync<ProjectDto>(response);
            project.Should().NotBeNull();
            project!.Id.Should().Be(testProject.Id);
            project.Title.Should().Be(testProject.Title);
            project.Description.Should().Be(testProject.Description);
        }

        [Fact]
        public async Task GetProjectById_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Act
            var response = await GetAsync("api/projects/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetFeaturedProjects_ReturnsOnlyFeaturedProjects()
        {
            // Arrange
            var featuredProject = _projectFaker.Generate();
            featuredProject.Featured = true;

            var nonFeaturedProject = _projectFaker.Generate();
            nonFeaturedProject.Featured = false;

            await _dbContext.Projects.AddRangeAsync(featuredProject, nonFeaturedProject);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/projects/featured");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projects = await DeserializeResponseAsync<IEnumerable<ProjectDto>>(response);
            projects.Should().HaveCount(1);
            projects.First().Id.Should().Be(featuredProject.Id);
            projects.First().Featured.Should().BeTrue();
        }

        [Fact]
        public async Task CreateProject_ReturnsCreatedProject_WhenValidDataProvided()
        {
            // Arrange
            var newProject = _projectFaker.Generate();

            // Act
            var response = await PostAsync("api/projects", newProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdProject = await DeserializeResponseAsync<ProjectDto>(response);
            createdProject.Should().NotBeNull();
            createdProject!.Title.Should().Be(newProject.Title);
            createdProject.Description.Should().Be(newProject.Description);
            createdProject.Id.Should().BeGreaterThan(0);
            createdProject.CreatedAt.Should().NotBe(default);
            createdProject.UpdatedAt.Should().NotBe(default);

            // Verify in database
            var dbProject = await _dbContext.Projects.FindAsync(createdProject.Id);
            dbProject.Should().NotBeNull();
            dbProject!.Title.Should().Be(newProject.Title);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenTitleIsEmpty()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.Title = string.Empty;

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenTitleExceedsMaxLength()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.Title = new string('A', 101); // Max length is 100

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenDescriptionIsEmpty()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.Description = string.Empty;

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenDescriptionExceedsMaxLength()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.Description = new string('A', 501); // Max length is 500

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenLiveUrlIsInvalid()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.LiveUrl = "not-a-valid-url";

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProject_ReturnsBadRequest_WhenGithubUrlIsInvalid()
        {
            // Arrange
            var invalidProject = _projectFaker.Generate();
            invalidProject.GithubUrl = "not-a-valid-url";

            // Act
            var response = await PostAsync("api/projects", invalidProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateProject_ReturnsNoContent_WhenValidDataProvided()
        {
            // Arrange
            var existingProject = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(existingProject);
            await _dbContext.SaveChangesAsync();

            var updatedProject = _projectFaker.Generate();
            updatedProject.Id = existingProject.Id;

            // Act
            var response = await PutAsync($"api/projects/{existingProject.Id}", updatedProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbProject = await _dbContext.Projects.FindAsync(existingProject.Id);
            dbProject.Should().NotBeNull();
            dbProject!.Title.Should().Be(updatedProject.Title);
            dbProject.Description.Should().Be(updatedProject.Description);
            dbProject.UpdatedAt.Should().BeAfter(dbProject.CreatedAt);
        }

        [Fact]
        public async Task UpdateProject_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var existingProject = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(existingProject);
            await _dbContext.SaveChangesAsync();

            var updatedProject = _projectFaker.Generate();
            updatedProject.Id = existingProject.Id + 1; // Different ID

            // Act
            var response = await PutAsync($"api/projects/{existingProject.Id}", updatedProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateProject_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            var updatedProject = _projectFaker.Generate();
            updatedProject.Id = 999;

            // Act
            var response = await PutAsync("api/projects/999", updatedProject);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteProject_ReturnsNoContent_WhenProjectExists()
        {
            // Arrange
            var existingProject = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(existingProject);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await DeleteAsync($"api/projects/{existingProject.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbProject = await _dbContext.Projects.FindAsync(existingProject.Id);
            dbProject.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProject_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Act
            var response = await DeleteAsync("api/projects/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task EndToEndProjectWorkflow_WorksCorrectly()
        {
            // Create a project
            var newProject = _projectFaker.Generate();
            var createResponse = await PostAsync("api/projects", newProject);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdProject = await DeserializeResponseAsync<ProjectDto>(createResponse);

            // Get the project
            var getResponse = await GetAsync($"api/projects/{createdProject!.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var retrievedProject = await DeserializeResponseAsync<ProjectDto>(getResponse);
            retrievedProject.Should().BeEquivalentTo(createdProject);

            // Update the project
            var updatedProject = _projectFaker.Generate();
            updatedProject.Id = createdProject.Id;
            var updateResponse = await PutAsync($"api/projects/{createdProject.Id}", updatedProject);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify update
            var verifyResponse = await GetAsync($"api/projects/{createdProject.Id}");
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var verifiedProject = await DeserializeResponseAsync<ProjectDto>(verifyResponse);
            verifiedProject!.Title.Should().Be(updatedProject.Title);
            verifiedProject.Description.Should().Be(updatedProject.Description);

            // Delete the project
            var deleteResponse = await DeleteAsync($"api/projects/{createdProject.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify deletion
            var finalResponse = await GetAsync($"api/projects/{createdProject.Id}");
            finalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}