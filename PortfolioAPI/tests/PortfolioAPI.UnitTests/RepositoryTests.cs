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
            var project = new Project { Id = 1, Title = "Test Project", Description = "Test Description" };
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
                new Project { Id = 1, Title = "Project 1", Description = "Description 1" },
                new Project { Id = 2, Title = "Project 2", Description = "Description 2" }
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
                new Project { Id = 1, Title = "Featured Project", IsFeatured = true },
                new Project { Id = 2, Title = "Regular Project", IsFeatured = false }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(p => p.IsFeatured);

            // Assert
            result.Should().ContainSingle();
            result.First().IsFeatured.Should().BeTrue();
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntity()
        {
            // Arrange
            var project = new Project { Title = "New Project", Description = "New Description" };

            // Act
            var result = await _repository.AddAsync(project);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Project");
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
            var project = new Project { Id = 1, Title = "Original Title", Description = "Original Description" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            project.Title = "Updated Title";

            // Act
            await _repository.UpdateAsync(project);

            // Assert
            var updatedProject = await _context.Projects.FindAsync(1);
            updatedProject.Should().NotBeNull();
            updatedProject!.Title.Should().Be("Updated Title");
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
            var project = new Project { Id = 1, Title = "Project to Delete", Description = "Description" };
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(project);

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
            var project = new Project { Id = 1, Title = "Existing Project", Description = "Description" };
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
                new Project { Id = 1, Title = "Project 1", Description = "Description 1" },
                new Project { Id = 2, Title = "Project 2", Description = "Description 2" },
                new Project { Id = 3, Title = "Project 3", Description = "Description 3" }
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
                new Project { Id = 1, Title = "Featured 1", IsFeatured = true },
                new Project { Id = 2, Title = "Regular 1", IsFeatured = false },
                new Project { Id = 3, Title = "Featured 2", IsFeatured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync(p => p.IsFeatured);

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
                projects.Add(new Project { Id = i, Title = $"Project {i}", Description = $"Description {i}" });
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
                projects.Add(new Project { Id = i, Title = $"Project {i}", Description = $"Description {i}", IsFeatured = i % 2 == 0 });
            }
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(p => p.IsFeatured, 1, 2);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(p => p.IsFeatured.Should().BeTrue());
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_ShouldReturnFirstMatchingEntity()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Project 1", IsFeatured = false },
                new Project { Id = 2, Title = "Project 2", IsFeatured = true },
                new Project { Id = 3, Title = "Project 3", IsFeatured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetFirstOrDefaultAsync(p => p.IsFeatured);

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
                new Project { Id = 1, Title = "Project 1", IsFeatured = false },
                new Project { Id = 2, Title = "Project 2", IsFeatured = true }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.AnyAsync(p => p.IsFeatured);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AnyAsync_ShouldReturnFalse_WhenNoEntityMatchesPredicate()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Title = "Project 1", IsFeatured = false },
                new Project { Id = 2, Title = "Project 2", IsFeatured = false }
            };
            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.AnyAsync(p => p.IsFeatured);

            // Assert
            result.Should().BeFalse();
        }
    }
}