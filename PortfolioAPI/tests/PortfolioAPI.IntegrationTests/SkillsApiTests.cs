using System.Net;
using Bogus;
using FluentAssertions;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class SkillsApiTests : TestBase
    {
        private readonly Faker<Skill> _skillFaker;

        public SkillsApiTests()
        {
            _skillFaker = new Faker<Skill>()
                .RuleFor(s => s.Name, f => f.Hacker.Noun())
                .RuleFor(s => s.Category, f => f.PickRandom("Frontend", "Backend", "Database", "DevOps", "Mobile", "Cloud"))
                .RuleFor(s => s.Proficiency, f => f.Random.Int(1, 100))
                .RuleFor(s => s.IconUrl, f => f.Image.PicsumUrl(32, 32))
                .RuleFor(s => s.IsActive, f => f.Random.Bool());
        }

        [Fact]
        public async Task GetAllSkills_ReturnsEmptyList_WhenNoSkillsExist()
        {
            // Act
            var response = await GetAsync("api/skills");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(response);
            skills.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllSkills_ReturnsAllSkills_WhenSkillsExist()
        {
            // Arrange
            var testSkills = _skillFaker.Generate(5);
            await _dbContext.Skills.AddRangeAsync(testSkills);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/skills");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(response);
            skills.Should().HaveCount(5);
            skills.Should().AllSatisfy(s =>
            {
                s.Id.Should().BeGreaterThan(0);
                s.Name.Should().NotBeNullOrEmpty();
                s.Proficiency.Should().BeInRange(1, 100);
                s.CreatedAt.Should().NotBe(default);
                s.UpdatedAt.Should().NotBe(default);
            });
        }

        [Fact]
        public async Task GetSkillById_ReturnsSkill_WhenSkillExists()
        {
            // Arrange
            var testSkill = _skillFaker.Generate();
            await _dbContext.Skills.AddAsync(testSkill);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync($"api/skills/{testSkill.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skill = await DeserializeResponseAsync<SkillDto>(response);
            skill.Should().NotBeNull();
            skill!.Id.Should().Be(testSkill.Id);
            skill.Name.Should().Be(testSkill.Name);
            skill.Category.Should().Be(testSkill.Category);
            skill.Proficiency.Should().Be(testSkill.Proficiency);
        }

        [Fact]
        public async Task GetSkillById_ReturnsNotFound_WhenSkillDoesNotExist()
        {
            // Act
            var response = await GetAsync("api/skills/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetSkillsByCategory_ReturnsFilteredSkills_WhenCategoryExists()
        {
            // Arrange
            var category = "Frontend";
            var frontendSkills = _skillFaker.Generate(3);
            frontendSkills.ForEach(s => s.Category = category);

            var backendSkills = _skillFaker.Generate(2);
            backendSkills.ForEach(s => s.Category = "Backend");

            await _dbContext.Skills.AddRangeAsync(frontendSkills);
            await _dbContext.Skills.AddRangeAsync(backendSkills);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync($"api/skills/category/{category}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(response);
            skills.Should().HaveCount(3);
            skills.Should().AllSatisfy(s => s.Category.Should().Be(category));
        }

        [Fact]
        public async Task GetSkillsByCategory_ReturnsEmptyList_WhenCategoryDoesNotExist()
        {
            // Act
            var response = await GetAsync("api/skills/category/NonExistentCategory");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(response);
            skills.Should().BeEmpty();
        }

        [Fact]
        public async Task GetVisibleSkills_ReturnsOnlyVisibleSkills()
        {
            // Arrange
            var visibleSkill = _skillFaker.Generate();
            visibleSkill.IsActive = true;

            var hiddenSkill = _skillFaker.Generate();
            hiddenSkill.IsActive = false;

            await _dbContext.Skills.AddRangeAsync(visibleSkill, hiddenSkill);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/skills/visible");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var skills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(response);
            skills.Should().HaveCount(1);
            skills.First().Id.Should().Be(visibleSkill.Id);
            skills.First().IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateSkill_ReturnsCreatedSkill_WhenValidDataProvided()
        {
            // Arrange
            var newSkill = _skillFaker.Generate();

            // Act
            var response = await PostAsync("api/skills", newSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSkill = await DeserializeResponseAsync<SkillDto>(response);
            createdSkill.Should().NotBeNull();
            createdSkill!.Name.Should().Be(newSkill.Name);
            createdSkill.Category.Should().Be(newSkill.Category);
            createdSkill.Proficiency.Should().Be(newSkill.Proficiency);
            createdSkill.IsActive.Should().Be(newSkill.IsActive);
            createdSkill.Id.Should().BeGreaterThan(0);
            createdSkill.CreatedAt.Should().NotBe(default);
            createdSkill.UpdatedAt.Should().NotBe(default);

            // Verify in database
            var dbSkill = await _dbContext.Skills.FindAsync(createdSkill.Id);
            dbSkill.Should().NotBeNull();
            dbSkill!.Name.Should().Be(newSkill.Name);
        }

        [Fact]
        public async Task CreateSkill_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            var invalidSkill = _skillFaker.Generate();
            invalidSkill.Name = string.Empty;

            // Act
            var response = await PostAsync("api/skills", invalidSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateSkill_ReturnsBadRequest_WhenNameExceedsMaxLength()
        {
            // Arrange
            var invalidSkill = _skillFaker.Generate();
            invalidSkill.Name = new string('A', 51); // Max length is 50

            // Act
            var response = await PostAsync("api/skills", invalidSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateSkill_ReturnsBadRequest_WhenCategoryExceedsMaxLength()
        {
            // Arrange
            var invalidSkill = _skillFaker.Generate();
            invalidSkill.Category = new string('A', 101); // Max length is 100

            // Act
            var response = await PostAsync("api/skills", invalidSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateSkill_ReturnsBadRequest_WhenProficiencyIsOutOfRange()
        {
            // Arrange - Test minimum value (should fail)
            var invalidSkill = _skillFaker.Generate();
            invalidSkill.Proficiency = 0; // Below minimum of 1

            // Act
            var response = await PostAsync("api/skills", invalidSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateSkill_ReturnsBadRequest_WhenProficiencyExceedsMaximum()
        {
            // Arrange - Test maximum value (should fail)
            var invalidSkill = _skillFaker.Generate();
            invalidSkill.Proficiency = 101; // Above maximum of 100

            // Act
            var response = await PostAsync("api/skills", invalidSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateSkill_ReturnsNoContent_WhenValidDataProvided()
        {
            // Arrange
            var existingSkill = _skillFaker.Generate();
            await _dbContext.Skills.AddAsync(existingSkill);
            await _dbContext.SaveChangesAsync();

            var updatedSkill = _skillFaker.Generate();
            updatedSkill.Id = existingSkill.Id;

            // Act
            var response = await PutAsync($"api/skills/{existingSkill.Id}", updatedSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbSkill = await _dbContext.Skills.FindAsync(existingSkill.Id);
            dbSkill.Should().NotBeNull();
            dbSkill!.Name.Should().Be(updatedSkill.Name);
            dbSkill.Category.Should().Be(updatedSkill.Category);
            dbSkill.Proficiency.Should().Be(updatedSkill.Proficiency);
            dbSkill.UpdatedAt.Should().BeAfter(dbSkill.CreatedAt);
        }

        [Fact]
        public async Task UpdateSkill_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var existingSkill = _skillFaker.Generate();
            await _dbContext.Skills.AddAsync(existingSkill);
            await _dbContext.SaveChangesAsync();

            var updatedSkill = _skillFaker.Generate();
            updatedSkill.Id = existingSkill.Id + 1; // Different ID

            // Act
            var response = await PutAsync($"api/skills/{existingSkill.Id}", updatedSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateSkill_ReturnsNotFound_WhenSkillDoesNotExist()
        {
            // Arrange
            var updatedSkill = _skillFaker.Generate();
            updatedSkill.Id = 999;

            // Act
            var response = await PutAsync("api/skills/999", updatedSkill);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteSkill_ReturnsNoContent_WhenSkillExists()
        {
            // Arrange
            var existingSkill = _skillFaker.Generate();
            await _dbContext.Skills.AddAsync(existingSkill);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await DeleteAsync($"api/skills/{existingSkill.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbSkill = await _dbContext.Skills.FindAsync(existingSkill.Id);
            dbSkill.Should().BeNull();
        }

        [Fact]
        public async Task DeleteSkill_ReturnsNotFound_WhenSkillDoesNotExist()
        {
            // Act
            var response = await DeleteAsync("api/skills/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task EndToEndSkillWorkflow_WorksCorrectly()
        {
            // Create a skill
            var newSkill = _skillFaker.Generate();
            var createResponse = await PostAsync("api/skills", newSkill);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdSkill = await DeserializeResponseAsync<SkillDto>(createResponse);

            // Get the skill
            var getResponse = await GetAsync($"api/skills/{createdSkill!.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var retrievedSkill = await DeserializeResponseAsync<SkillDto>(getResponse);
            retrievedSkill.Should().BeEquivalentTo(createdSkill);

            // Update the skill
            var updatedSkill = _skillFaker.Generate();
            updatedSkill.Id = createdSkill.Id;
            var updateResponse = await PutAsync($"api/skills/{createdSkill.Id}", updatedSkill);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify update
            var verifyResponse = await GetAsync($"api/skills/{createdSkill.Id}");
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var verifiedSkill = await DeserializeResponseAsync<SkillDto>(verifyResponse);
            verifiedSkill!.Name.Should().Be(updatedSkill.Name);
            verifiedSkill.Category.Should().Be(updatedSkill.Category);

            // Delete the skill
            var deleteResponse = await DeleteAsync($"api/skills/{createdSkill.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify deletion
            var finalResponse = await GetAsync($"api/skills/{createdSkill.Id}");
            finalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SkillsMaintainDataIntegrity_AcrossMultipleOperations()
        {
            // Arrange - Create multiple skills
            var skills = _skillFaker.Generate(10);
            foreach (var skill in skills)
            {
                var response = await PostAsync("api/skills", skill);
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }

            // Act - Get all skills
            var getAllResponse = await GetAsync("api/skills");
            getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var allSkills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(getAllResponse);

            // Assert - Verify all skills are returned
            allSkills.Should().HaveCount(10);

            // Act - Update all skills to be visible
            foreach (var skill in allSkills!)
            {
                var updatedSkill = new Skill
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Category = skill.Category,
                    Proficiency = skill.Proficiency,
                    IconUrl = skill.IconUrl,
                    IsActive = true
                };

                var updateResponse = await PutAsync($"api/skills/{skill.Id}", updatedSkill);
                updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            // Assert - Verify all skills are now visible
            var visibleResponse = await GetAsync("api/skills/visible");
            visibleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var visibleSkills = await DeserializeResponseAsync<IEnumerable<SkillDto>>(visibleResponse);
            visibleSkills.Should().HaveCount(10);
            visibleSkills.Should().AllSatisfy(s => s.IsActive.Should().BeTrue());
        }
    }
}