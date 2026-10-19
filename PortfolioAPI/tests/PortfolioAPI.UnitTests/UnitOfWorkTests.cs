using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
                .Options;

            _context = new PortfolioDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
        }

        public void Dispose()
        {
            // The container owns the DbContext lifetime — UnitOfWork no longer
            // exposes Dispose (there is no transaction API either; see the
            // UnitOfWork doc comment), so the test disposes the context itself.
            _context.Dispose();
        }

        private static PortfolioDbContext CreateContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<PortfolioDbContext>()
                .UseInMemoryDatabase(databaseName)
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
        public async Task SaveChangesAsync_ShouldPersistEveryStagedAggregate()
        {
            // Arrange — two entities staged through two different repositories
            // over the SAME context, committed by the single SaveChanges call.
            await _unitOfWork.Projects.AddAsync(new Project { Title = "Project 1", Description = "Description 1" });
            await _unitOfWork.Skills.AddAsync(new Skill { Name = "C#", Category = "Languages", Proficiency = 90 });

            // Act
            var affected = await _unitOfWork.SaveChangesAsync();

            // Assert — visible through a separate context on the same store
            affected.Should().Be(2);
            using var assertContext = CreateContext(_databaseName);
            (await assertContext.Projects.CountAsync()).Should().Be(1);
            (await assertContext.Skills.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldNotPersist_WhenNothingWasCommitted()
        {
            // Arrange — staged but never committed
            await _unitOfWork.Projects.AddAsync(new Project { Title = "Unsaved", Description = "Pending" });

            // Act — no SaveChangesAsync call

            // Assert — a fresh context over the same store sees nothing
            using var assertContext = CreateContext(_databaseName);
            (await assertContext.Projects.CountAsync()).Should().Be(0);
        }
    }
}
