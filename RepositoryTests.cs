using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using System.Linq.Expressions;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class RepositoryTests : IDisposable
    {
        private readonly PortfolioDbContext _context;
        private readonly Repository<Project> _repository;

        public RepositoryTests()
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PortfolioDbContext(options);
            _repository = new Repository<Project>(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Test Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ø§Ø®ØªØ¨Ø§Ø±", DescriptionEn = "Test Description", DescriptionAr = "ÙˆØµÙ Ø§Ø®ØªØ¨Ø§Ø±" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(project);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Project 1", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 1", DescriptionEn = "Description 1", DescriptionAr = "ÙˆØµÙ 1" },
                new Project { Id = 2, TitleEn = "Project 2", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 2", DescriptionEn = "Description 2", DescriptionAr = "ÙˆØµÙ 2" }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(projects);
        }

        [Fact]
        public async Task FindAsync_ShouldReturnFilteredEntities()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Featured Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ù…Ù…ÙŠØ²", Featured = true },
                new Project { Id = 2, TitleEn = "Regular Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ø¹Ø§Ø¯ÙŠ", Featured = false }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(p => p.Featured);

            // Assert
            result.Should().ContainSingle();
            result.First().Featured.Should().BeTrue();
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            // Arrange
            var project = new Project { TitleEn = "New Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ø¬Ø¯ÙŠØ¯", DescriptionEn = "New Description", DescriptionAr = "ÙˆØµÙ Ø¬Ø¯ÙŠØ¯" };

            // Act
            var result = await _repository.AddAsync(project);
            // Repository only tracks the entity; persistence happens on save.
            await _context.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.TitleEn.Should().Be("New Project");
            result.CreatedAt.Should().NotBe(default);
            _context.Projects.Should().Contain(result);
        }

        [Fact]
        public async Task AddAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
        {
            // Act
            Func<Task> act = async () => await _repository.AddAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Original Title", TitleAr = "Ø¹Ù†ÙˆØ§Ù† Ø£ØµÙ„ÙŠ", DescriptionEn = "Original Description", DescriptionAr = "ÙˆØµÙ Ø£ØµÙ„ÙŠ" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            project.TitleEn = "Updated Title";
            project.TitleAr = "Ø¹Ù†ÙˆØ§Ù† Ù…Ø­Ø¯Ø«";

            // Act
            await _repository.UpdateAsync(project);

            // Assert
            var updatedProject = await _context.Projects.FindAsync(1);
            updatedProject.Should().NotBeNull();
            updatedProject!.TitleEn.Should().Be("Updated Title");
            updatedProject.TitleAr.Should().Be("Ø¹Ù†ÙˆØ§Ù† Ù…Ø­Ø¯Ø«");
            updatedProject.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
        {
            // Act
            Func<Task> act = async () => await _repository.UpdateAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEntity()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Project to Delete", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ù„Ù„Ø­Ø°Ù", DescriptionEn = "Description", DescriptionAr = "ÙˆØµÙ" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(project);
            // Remove() only marks the entity; persistence happens on save.
            await _context.SaveChangesAsync();

            // Assert
            _context.Projects.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
        {
            // Act
            Func<Task> act = async () => await _repository.DeleteAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
        {
            // Arrange
            var project = new Project { Id = 1, TitleEn = "Existing Project", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ Ù…ÙˆØ¬ÙˆØ¯", DescriptionEn = "Description", DescriptionAr = "ÙˆØµÙ" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
        {
            // Act
            var result = await _repository.ExistsAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnTotalCount()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Project 1", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 1", DescriptionEn = "Description 1", DescriptionAr = "ÙˆØµÙ 1" },
                new Project { Id = 2, TitleEn = "Project 2", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 2", DescriptionEn = "Description 2", DescriptionAr = "ÙˆØµÙ 2" },
                new Project { Id = 3, TitleEn = "Project 3", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 3", DescriptionEn = "Description 3", DescriptionAr = "ÙˆØµÙ 3" }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync();

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public async Task CountAsync_WithPredicate_ShouldReturnFilteredCount()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Featured 1", TitleAr = "Ù…Ù…ÙŠØ² 1", Featured = true },
                new Project { Id = 2, TitleEn = "Regular 1", TitleAr = "Ø¹Ø§Ø¯ÙŠ 1", Featured = false },
                new Project { Id = 3, TitleEn = "Featured 2", TitleAr = "Ù…Ù…ÙŠØ² 2", Featured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync(p => p.Featured);

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldReturnPagedResults()
        {
            // Arrange
            var projects = new List<Project>();
            for (int i = 1; i <= 10; i++)
            {
                projects.Add(new Project { Id = i, TitleEn = $"Project {i}", TitleAr = $"Ù…Ø´Ø±ÙˆØ¹ {i}", DescriptionEn = $"Description {i}", DescriptionAr = $"ÙˆØµÙ {i}" });
            }
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(2, 3);

            // Assert
            result.Should().HaveCount(3);
            result.First().Id.Should().Be(4);
            result.Last().Id.Should().Be(6);
        }

        [Fact]
        public async Task GetPagedAsync_WithPredicate_ShouldReturnFilteredPagedResults()
        {
            // Arrange
            var projects = new List<Project>();
            for (int i = 1; i <= 10; i++)
            {
                projects.Add(new Project { Id = i, TitleEn = $"Project {i}", TitleAr = $"Ù…Ø´Ø±ÙˆØ¹ {i}", DescriptionEn = $"Description {i}", DescriptionAr = $"ÙˆØµÙ {i}", Featured = i % 2 == 0 });
            }
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(p => p.Featured, 1, 2);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.Featured.Should().BeTrue());
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_ShouldReturnFirstMatchingEntity()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Project 1", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 1", Featured = false },
                new Project { Id = 2, TitleEn = "Project 2", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 2", Featured = true },
                new Project { Id = 3, TitleEn = "Project 3", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 3", Featured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetFirstOrDefaultAsync(p => p.Featured);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(2);
        }

        [Fact]
        public async Task AnyAsync_ShouldReturnTrue_WhenAnyEntityMatchesPredicate()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Project 1", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 1", Featured = false },
                new Project { Id = 2, TitleEn = "Project 2", TitleAr = "Ù…Ø´Ø±ÙˆØ¹ 2", Featured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.AnyAsync(p => p.Featured);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AnyAsync_ShouldReturnFalse_WhenNoEntityMatchesPredicate()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, TitleEn = "Project 1", TitleAr = "مشروع 1", Featured = false },
                new Project { Id = 2, TitleEn = "Project 2", TitleAr = "مشروع 2", Featured = false }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.AnyAsync(p => p.Featured);

            // Assert
            result.Should().BeFalse();
        }
    }
}