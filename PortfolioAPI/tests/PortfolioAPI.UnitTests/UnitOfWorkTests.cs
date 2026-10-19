using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly PortfolioDbContext _context;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PortfolioDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
        }

        public void Dispose()
        {
            _unitOfWork.Dispose();
            _context.Dispose();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Act
            Action act = () => new UnitOfWork(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Projects_ShouldReturnRepositoryInstance()
        {
            // Act
            var repository = _unitOfWork.Projects;

            // Assert
            repository.Should().NotBeNull();
            repository.Should().BeOfType<Repository<Project>>();
        }

        [Fact]
        public void Skills_ShouldReturnRepositoryInstance()
        {
            // Act
            var repository = _unitOfWork.Skills;

            // Assert
            repository.Should().NotBeNull();
            repository.Should().BeOfType<Repository<Skill>>();
        }

        [Fact]
        public void Experiences_ShouldReturnRepositoryInstance()
        {
            // Act
            var repository = _unitOfWork.Experiences;

            // Assert
            repository.Should().NotBeNull();
            repository.Should().BeOfType<Repository<Experience>>();
        }

        [Fact]
        public void Contacts_ShouldReturnRepositoryInstance()
        {
            // Act
            var repository = _unitOfWork.Contacts;

            // Assert
            repository.Should().NotBeNull();
            repository.Should().BeOfType<Repository<Contact>>();
        }

        [Fact]
        public void Testimonials_ShouldReturnRepositoryInstance()
        {
            // Act
            var repository = _unitOfWork.Testimonials;

            // Assert
            repository.Should().NotBeNull();
            repository.Should().BeOfType<Repository<Testimonial>>();
        }

        [Fact]
        public void Projects_ShouldReturnSameInstance_OnMultipleCalls()
        {
            // Act
            var repository1 = _unitOfWork.Projects;
            var repository2 = _unitOfWork.Projects;

            // Assert
            repository1.Should().BeSameAs(repository2);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldReturnAffectedRowsCount()
        {
            // Arrange
            var project = new Project { Title = "Test Project", Description = "Test Description" };
            await _unitOfWork.Projects.AddAsync(project);

            // Act
            var result = await _unitOfWork.SaveChangesAsync();

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task BeginTransactionAsync_ShouldStartTransaction()
        {
            // Act
            await _unitOfWork.BeginTransactionAsync();

            // Assert
            // Transaction should be started, but we can't directly verify it with in-memory database
            // This test ensures no exceptions are thrown
        }

        [Fact]
        public async Task CommitTransactionAsync_ShouldCommitTransaction()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();
            var project = new Project { Title = "Test Project", Description = "Test Description" };
            await _unitOfWork.Projects.AddAsync(project);

            // Act
            await _unitOfWork.CommitTransactionAsync();

            // Assert
            var savedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id);
            savedProject.Should().NotBeNull();
        }

        [Fact]
        public async Task RollbackTransactionAsync_ShouldRollbackTransaction()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();
            var project = new Project { Title = "Test Project", Description = "Test Description" };
            await _unitOfWork.Projects.AddAsync(project);

            // Act
            await _unitOfWork.RollbackTransactionAsync();

            // Assert
            var savedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id);
            savedProject.Should().BeNull();
        }

        [Fact]
        public void Dispose_ShouldDisposeContext()
        {
            // Arrange
            var mockContext = new Mock<PortfolioDbContext>();
            var unitOfWork = new UnitOfWork(mockContext.Object);

            // Act
            unitOfWork.Dispose();

            // Assert
            mockContext.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldHandleMultipleCalls()
        {
            // Arrange
            var mockContext = new Mock<PortfolioDbContext>();
            var unitOfWork = new UnitOfWork(mockContext.Object);

            // Act
            unitOfWork.Dispose();
            unitOfWork.Dispose();

            // Assert
            mockContext.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task TransactionOperations_ShouldWorkTogether()
        {
            // Arrange
            var project1 = new Project { Title = "Project 1", Description = "Description 1" };
            var project2 = new Project { Title = "Project 2", Description = "Description 2" };

            // Act
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Projects.AddAsync(project1);
            await _unitOfWork.Projects.AddAsync(project2);
            await _unitOfWork.CommitTransactionAsync();

            // Assert
            var projects = await _unitOfWork.Projects.GetAllAsync();
            projects.Should().HaveCount(2);
        }

        [Fact]
        public async Task TransactionRollback_ShouldNotPersistChanges()
        {
            // Arrange
            var project1 = new Project { Title = "Project 1", Description = "Description 1" };
            var project2 = new Project { Title = "Project 2", Description = "Description 2" };

            // Act
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Projects.AddAsync(project1);
            await _unitOfWork.Projects.AddAsync(project2);
            await _unitOfWork.RollbackTransactionAsync();

            // Assert
            var projects = await _unitOfWork.Projects.GetAllAsync();
            projects.Should().BeEmpty();
        }
    }
}