using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Infrastructure.Data;
using PortfolioAPI.Infrastructure.Repositories;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class UnitOfWorkTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly PortfolioDbContext _context;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                // InMemory has no real transactions; without this, Begin/Commit/Rollback throw.
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new PortfolioDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
        }

        public void Dispose()
        {
            _unitOfWork.Dispose();
            _context.Dispose();
        }

        private static PortfolioDbContext CreateContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new PortfolioDbContext(options);
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
            repository.Should().BeOfType<ProjectRepository>();
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
            // InMemory ignores transactions (warning configured off); this test
            // asserts the call completes without throwing.
        }

        [Fact]
        public async Task CommitTransactionAsync_ShouldCommitTransaction()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();
            var project = new Project { Title = "Test Project", Description = "Test Description" };
            await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveChangesAsync();

            // Act
            await _unitOfWork.CommitTransactionAsync();

            // Assert — verify via a separate context on the same store
            using var assertContext = CreateContext(_databaseName);
            var savedProject = await assertContext.Projects.FindAsync(project.Id);
            savedProject.Should().NotBeNull();
            savedProject!.Title.Should().Be("Test Project");
        }

        [Fact]
        public async Task RollbackTransactionAsync_ShouldNotPersistChanges()
        {
            // Arrange
            await _unitOfWork.BeginTransactionAsync();
            var project = new Project { Title = "Test Project", Description = "Test Description" };
            await _unitOfWork.Projects.AddAsync(project);

            // Act
            await _unitOfWork.RollbackTransactionAsync();

            // Assert — nothing was saved, so the store must stay empty. The
            // change tracker is cleared first because FindAsync consults it
            // before the store and InMemory has no real transaction to roll back.
            _context.ChangeTracker.Clear();
            var savedProject = await _unitOfWork.Projects.GetByIdAsync(project.Id);
            savedProject.Should().BeNull();
        }

        [Fact]
        public void Dispose_ShouldDisposeContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new PortfolioDbContext(options);
            var unitOfWork = new UnitOfWork(context);

            // Act
            unitOfWork.Dispose();

            // Assert — a disposed context rejects further use
            Action act = () => context.Projects.ToList();
            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_ShouldHandleMultipleCalls()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new PortfolioDbContext(options);
            var unitOfWork = new UnitOfWork(context);

            // Act
            Action act = () =>
            {
                unitOfWork.Dispose();
                unitOfWork.Dispose();
            };

            // Assert — must not throw
            act.Should().NotThrow();
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
            await _unitOfWork.SaveChangesAsync();
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

            // Assert — nothing was saved, so the store stays empty
            var projects = await _unitOfWork.Projects.GetAllAsync();
            projects.Should().BeEmpty();
        }
    }
}
