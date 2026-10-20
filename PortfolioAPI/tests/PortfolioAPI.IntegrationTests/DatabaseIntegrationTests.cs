using System.Data;
using System.Net;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class DatabaseIntegrationTests : TestBase
    {
        private readonly Faker<Project> _projectFaker;
        private readonly Faker<Skill> _skillFaker;
        private readonly Faker<Contact> _contactFaker;

        public DatabaseIntegrationTests()
        {
            _projectFaker = new Faker<Project>()
                .RuleFor(p => p.TitleEn, f => f.Lorem.Sentence(3))
                .RuleFor(p => p.TitleAr, f => f.Lorem.Sentence(3))
                .RuleFor(p => p.DescriptionEn, f => f.Lorem.Paragraphs(2))
                .RuleFor(p => p.DescriptionAr, f => f.Lorem.Paragraphs(2))
                .RuleFor(p => p.Technologies, f => f.Make(3, () => f.Hacker.Noun()))
                .RuleFor(p => p.LiveUrl, f => f.Internet.Url())
                .RuleFor(p => p.GithubUrl, f => f.Internet.Url())
                .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(p => p.Featured, f => f.Random.Bool());

            _skillFaker = new Faker<Skill>()
                .RuleFor(s => s.NameEn, f => f.Hacker.Noun())
                .RuleFor(s => s.NameAr, f => f.Hacker.Noun())
                .RuleFor(s => s.CategoryEn, f => f.PickRandom("Frontend", "Backend", "Database", "DevOps"))
                .RuleFor(s => s.CategoryAr, f => f.PickRandom("الواجهة الأمامية", "الخلفية", "قاعدة البيانات", "ديف أوبس"))
                .RuleFor(s => s.Proficiency, f => f.Random.Int(1, 100))
                .RuleFor(s => s.IconUrl, f => f.Image.PicsumUrl(32, 32))
                .RuleFor(s => s.IsActive, f => f.Random.Bool());

            _contactFaker = new Faker<Contact>()
                .RuleFor(c => c.Name, f => f.Name.FullName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.Subject, f => f.Lorem.Sentence(5))
                .RuleFor(c => c.Message, f => f.Lorem.Paragraphs(2))
                .RuleFor(c => c.IsRead, f => false);
        }

        [Fact]
        public async Task DatabaseConstraints_ProjectTitle_RequiredAndUnique()
        {
            // Arrange
            var project1 = _projectFaker.Generate();
            project1.TitleEn = "Unique Project Title";

            var project2 = _projectFaker.Generate();
            project2.TitleEn = "Unique Project Title"; // Same title

            // Act & Assert
            await _dbContext.Projects.AddAsync(project1);
            await _dbContext.SaveChangesAsync();

            // Attempt to add project with duplicate title
            await _dbContext.Projects.AddAsync(project2);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            exception.InnerException.Should().NotBeNull();
            exception.InnerException!.Message.Should().Contain("constraint");
        }

        [Fact]
        public async Task DatabaseConstraints_ProjectDescription_Required()
        {
            // Arrange
            var project = _projectFaker.Generate();
            project.DescriptionEn = null!;

            // Act & Assert
            await _dbContext.Projects.AddAsync(project);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task DatabaseConstraints_SkillName_RequiredAndUnique()
        {
            // Arrange
            var skill1 = _skillFaker.Generate();
            skill1.NameEn = "Unique Skill Name";

            var skill2 = _skillFaker.Generate();
            skill2.NameEn = "Unique Skill Name"; // Same name

            // Act & Assert
            await _dbContext.Skills.AddAsync(skill1);
            await _dbContext.SaveChangesAsync();

            // Attempt to add skill with duplicate name
            await _dbContext.Skills.AddAsync(skill2);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            exception.InnerException.Should().NotBeNull();
            exception.InnerException!.Message.Should().Contain("constraint");
        }

        [Fact]
        public async Task DatabaseConstraints_ContactNameAndEmail_Required()
        {
            // Arrange
            var contact = _contactFaker.Generate();
            contact.Name = null!;
            contact.Email = null!;

            // Act & Assert
            await _dbContext.Contacts.AddAsync(contact);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task DatabaseConstraints_ContactMessage_Required()
        {
            // Arrange
            var contact = _contactFaker.Generate();
            contact.Message = null!;

            // Act & Assert
            await _dbContext.Contacts.AddAsync(contact);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task DatabaseTransactions_RollbackOnFailure_ProjectCreation()
        {
            // Arrange
            var validProject = _projectFaker.Generate();
            var invalidProject = _projectFaker.Generate();
            invalidProject.TitleEn = null!; // This will cause a constraint violation

            // Act & Assert
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.Projects.AddAsync(validProject);
                await _dbContext.Projects.AddAsync(invalidProject);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                Assert.Fail("Expected transaction to fail due to constraint violation");
            }
            catch (DbUpdateException)
            {
                // Expected exception
                await transaction.RollbackAsync();

                // Verify rollback - valid project should not be in database
                var projectCount = await _dbContext.Projects.CountAsync();
                projectCount.Should().Be(0);
            }
        }

        [Fact]
        public async Task DatabaseTransactions_CommitOnSuccess_MultipleEntities()
        {
            // Arrange
            var project = _projectFaker.Generate();
            var skill = _skillFaker.Generate();
            var contact = _contactFaker.Generate();

            // Act
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.Projects.AddAsync(project);
                await _dbContext.Skills.AddAsync(skill);
                await _dbContext.Contacts.AddAsync(contact);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                Assert.Fail("Transaction should have succeeded");
            }

            // Assert - Verify all entities were committed
            var projectCount = await _dbContext.Projects.CountAsync();
            var skillCount = await _dbContext.Skills.CountAsync();
            var contactCount = await _dbContext.Contacts.CountAsync();

            projectCount.Should().Be(1);
            skillCount.Should().Be(1);
            contactCount.Should().Be(1);
        }

        [Fact]
        public async Task DatabaseRelationships_BaseEntityTimestamps_AutoGenerated()
        {
            // Arrange
            var project = _projectFaker.Generate();

            // Act
            var beforeAdd = DateTime.UtcNow;
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();
            var afterAdd = DateTime.UtcNow;

            // Assert
            project.CreatedAt.Should().BeOnOrAfter(beforeAdd);
            project.CreatedAt.Should().BeOnOrBefore(afterAdd);
            project.UpdatedAt.Should().BeOnOrAfter(beforeAdd);
            project.UpdatedAt.Should().BeOnOrBefore(afterAdd);
            project.CreatedAt.Should().Be(project.UpdatedAt);
        }

        [Fact]
        public async Task DatabaseRelationships_BaseEntityTimestamps_UpdateOnModification()
        {
            // Arrange
            var project = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();

            var originalCreatedAt = project.CreatedAt;
            var originalUpdatedAt = project.UpdatedAt;

            await Task.Delay(100); // Ensure timestamp difference

            // Act
            project.DescriptionEn = "Updated Description";
            await _dbContext.SaveChangesAsync();

            // Assert
            project.CreatedAt.Should().Be(originalCreatedAt);
            project.UpdatedAt.Should().BeAfter(originalUpdatedAt!.Value);
        }

        [Fact]
        public async Task DatabaseIndexes_Performance_QueryOptimization()
        {
            // Arrange - Create many records
            var projects = _projectFaker.Generate(100);
            var skills = _skillFaker.Generate(100);
            var contacts = _contactFaker.Generate(100);

            await _dbContext.Projects.AddRangeAsync(projects);
            await _dbContext.Skills.AddRangeAsync(skills);
            await _dbContext.Contacts.AddRangeAsync(contacts);
            await _dbContext.SaveChangesAsync();

            // Act & Assert - Query operations should be fast (basic performance check)
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var featuredProjects = await _dbContext.Projects.Where(p => p.Featured).ToListAsync();
            var visibleSkills = await _dbContext.Skills.Where(s => s.IsActive).ToListAsync();
            var unreadContacts = await _dbContext.Contacts.Where(c => !c.IsRead).ToListAsync();

            watch.Stop();

            // Basic performance assertion - should complete in reasonable time
            watch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max

            // Verify query results
            featuredProjects.Should().NotBeNull();
            visibleSkills.Should().NotBeNull();
            unreadContacts.Should().NotBeNull();
        }

        [Fact]
        public async Task DatabaseConcurrency_HandlesConcurrentUpdates()
        {
            // Arrange
            var project = _projectFaker.Generate();
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();

            // Simulate concurrent updates
            var task1 = Task.Run(async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var dbContext1 = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                var proj1 = await dbContext1.Projects.FindAsync(project.Id);
                proj1!.TitleEn = "Updated by Task 1";
                await dbContext1.SaveChangesAsync();
            });

            var task2 = Task.Run(async () =>
            {
                await Task.Delay(50); // Slight delay to ensure task1 starts first
                using var scope = _factory.Services.CreateScope();
                var dbContext2 = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                var proj2 = await dbContext2.Projects.FindAsync(project.Id);
                proj2!.TitleEn = "Updated by Task 2";
                await dbContext2.SaveChangesAsync();
            });

            // Act
            await Task.WhenAll(task1, task2);

            // Assert - One of the updates should succeed
            var finalProject = await _dbContext.Projects.FindAsync(project.Id);
            finalProject.Should().NotBeNull();
            finalProject!.TitleEn.Should().BeOneOf("Updated by Task 1", "Updated by Task 2");
        }

        [Fact]
        public async Task DatabaseDataIntegrity_ForeignKeyConstraints()
        {
            // This test verifies that if there were foreign key relationships,
            // they would be properly enforced. Since this schema doesn't have
            // explicit FK constraints beyond what's auto-generated, we'll test
            // the integrity of the data relationships through business logic.

            // Arrange
            var projects = _projectFaker.Generate(10);
            var skills = _skillFaker.Generate(10);
            var contacts = _contactFaker.Generate(10);

            // Act
            await _dbContext.Projects.AddRangeAsync(projects);
            await _dbContext.Skills.AddRangeAsync(skills);
            await _dbContext.Contacts.AddRangeAsync(contacts);
            await _dbContext.SaveChangesAsync();

            // Assert - Verify all entities have valid IDs and relationships
            var savedProjects = await _dbContext.Projects.ToListAsync();
            var savedSkills = await _dbContext.Skills.ToListAsync();
            var savedContacts = await _dbContext.Contacts.ToListAsync();

            savedProjects.Should().HaveCount(10);
            savedSkills.Should().HaveCount(10);
            savedContacts.Should().HaveCount(10);

            savedProjects.Should().AllSatisfy(p => p.Id.Should().BeGreaterThan(0));
            savedSkills.Should().AllSatisfy(s => s.Id.Should().BeGreaterThan(0));
            savedContacts.Should().AllSatisfy(c => c.Id.Should().BeGreaterThan(0));
        }

        [Fact]
        public async Task DatabaseBulkOperations_HandlesLargeDataSets()
        {
            // Arrange
            const int bulkSize = 1000;
            var projects = _projectFaker.Generate(bulkSize);
            var skills = _skillFaker.Generate(bulkSize);
            var contacts = _contactFaker.Generate(bulkSize);

            // Act
            var watch = System.Diagnostics.Stopwatch.StartNew();

            await _dbContext.Projects.AddRangeAsync(projects);
            await _dbContext.Skills.AddRangeAsync(skills);
            await _dbContext.Contacts.AddRangeAsync(contacts);
            await _dbContext.SaveChangesAsync();

            watch.Stop();

            // Assert - Bulk insert should complete in reasonable time
            watch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds max

            var projectCount = await _dbContext.Projects.CountAsync();
            var skillCount = await _dbContext.Skills.CountAsync();
            var contactCount = await _dbContext.Contacts.CountAsync();

            projectCount.Should().Be(bulkSize);
            skillCount.Should().Be(bulkSize);
            contactCount.Should().Be(bulkSize);
        }

        [Fact]
        public async Task DatabaseConnection_Isolation_TestContainers()
        {
            // This test verifies that each test runs in isolation
            // by checking that the database is clean at the start

            // Assert - Database should be empty at test start
            var projectCount = await _dbContext.Projects.CountAsync();
            var skillCount = await _dbContext.Skills.CountAsync();
            var contactCount = await _dbContext.Contacts.CountAsync();

            projectCount.Should().Be(0);
            skillCount.Should().Be(0);
            contactCount.Should().Be(0);

            // Act - Add some data
            var project = _projectFaker.Generate();
            var skill = _skillFaker.Generate();
            var contact = _contactFaker.Generate();

            await _dbContext.Projects.AddAsync(project);
            await _dbContext.Skills.AddAsync(skill);
            await _dbContext.Contacts.AddAsync(contact);
            await _dbContext.SaveChangesAsync();

            // Assert - Data should be present
            var newProjectCount = await _dbContext.Projects.CountAsync();
            var newSkillCount = await _dbContext.Skills.CountAsync();
            var newContactCount = await _dbContext.Contacts.CountAsync();

            newProjectCount.Should().Be(1);
            newSkillCount.Should().Be(1);
            newContactCount.Should().Be(1);
        }
    }
}