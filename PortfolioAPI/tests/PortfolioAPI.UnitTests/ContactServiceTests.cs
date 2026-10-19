using FluentAssertions;
using Moq;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Services;
using Xunit;
using FluentValidation;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.UnitTests
{
    public class ContactServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<Contact>> _validatorMock;
        private readonly ContactService _contactService;

        public ContactServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<Contact>>();
            _contactService = new ContactService(_unitOfWorkMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task GetAllContactsAsync_ShouldReturnAllContacts()
        {
            // Arrange
            var contacts = new List<Contact>
            {
                new Contact { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new Contact { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
            };
            _unitOfWorkMock.Setup(u => u.Contacts.GetAllAsync()).ReturnsAsync(contacts);

            // Act
            var result = await _contactService.GetAllContactsAsync();

            // Assert
            result.Should().BeEquivalentTo(contacts);
            _unitOfWorkMock.Verify(u => u.Contacts.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetContactByIdAsync_ShouldReturnContact_WhenContactExists()
        {
            // Arrange
            var contact = new Contact { Id = 1, Name = "John Doe", Email = "john@example.com" };
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);

            // Act
            var result = await _contactService.GetContactByIdAsync(1);

            // Assert
            result.Should().Be(contact);
            _unitOfWorkMock.Verify(u => u.Contacts.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetContactByIdAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync((Contact?)null);

            // Act
            Func<Task> act = async () => await _contactService.GetContactByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Contact with id 1 not found.");
        }

        [Fact]
        public async Task GetUnreadContactsAsync_ShouldReturnUnreadContacts()
        {
            // Arrange
            var contacts = new List<Contact>
            {
                new Contact { Id = 1, Name = "John", IsRead = false },
                new Contact { Id = 2, Name = "Jane", IsRead = true }
            };
            _unitOfWorkMock.Setup(u => u.Contacts.FindAsync(c => !c.IsRead))
                .ReturnsAsync(contacts.Where(c => !c.IsRead));

            // Act
            var result = await _contactService.GetUnreadContactsAsync();

            // Assert
            result.Should().ContainSingle(c => !c.IsRead);
            _unitOfWorkMock.Verify(u => u.Contacts.FindAsync(c => !c.IsRead), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ShouldCreateContact_WhenValidationPasses()
        {
            // Arrange
            var contact = new Contact { Name = "New Contact", Email = "new@example.com", Message = "Hello" };
            var validationResult = new FluentValidation.Results.ValidationResult();
            var createdContact = new Contact { Id = 1, Name = "New Contact", Email = "new@example.com", Message = "Hello", CreatedAt = DateTime.UtcNow };

            _validatorMock.Setup(v => v.ValidateAsync(contact, default)).ReturnsAsync(validationResult);
            _unitOfWorkMock.Setup(u => u.Contacts.AddAsync(It.IsAny<Contact>())).ReturnsAsync(createdContact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _contactService.CreateContactAsync(contact);

            // Assert
            result.Should().Be(createdContact);
            _validatorMock.Verify(v => v.ValidateAsync(contact, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Contacts.AddAsync(It.IsAny<Contact>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var contact = new Contact { Name = "", Email = "invalid", Message = "" };
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Name", "Name is required.")
            };
            var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);

            _validatorMock.Setup(v => v.ValidateAsync(contact, default)).ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _contactService.CreateContactAsync(contact);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _validatorMock.Verify(v => v.ValidateAsync(contact, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.Contacts.AddAsync(It.IsAny<Contact>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkContactAsRead_WhenContactExists()
        {
            // Arrange
            var contact = new Contact { Id = 1, Name = "John", IsRead = false };
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _contactService.MarkAsReadAsync(1);

            // Assert
            contact.IsRead.Should().BeTrue();
            contact.ReadAt.Should().NotBeNull();
            _unitOfWorkMock.Verify(u => u.Contacts.GetByIdAsync(1), Times.Once);
            _unitOfWorkMock.Verify(u => u.Contacts.UpdateAsync(contact), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync((Contact?)null);

            // Act
            Func<Task> act = async () => await _contactService.MarkAsReadAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Contact with id 1 not found.");
        }

        [Fact]
        public async Task DeleteContactAsync_ShouldDeleteContact_WhenContactExists()
        {
            // Arrange
            var contact = new Contact { Id = 1, Name = "Contact to Delete" };
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _contactService.DeleteContactAsync(1);

            // Assert
            _unitOfWorkMock.Verify(u => u.Contacts.GetByIdAsync(1), Times.Once);
            _unitOfWorkMock.Verify(u => u.Contacts.DeleteAsync(contact), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteContactAsync_ShouldThrowNotFoundException_WhenContactDoesNotExist()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync((Contact?)null);

            // Act
            Func<Task> act = async () => await _contactService.DeleteContactAsync(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Contact with id 1 not found.");
        }
    }
}