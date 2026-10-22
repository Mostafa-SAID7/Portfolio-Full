using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Application.Services;
using Xunit;
using System.Linq.Expressions;
using FluentValidation;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.UnitTests
{
    public class ContactServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<Contact>> _validatorMock;
        private readonly Mock<IValidator<ContactReply>> _replyValidatorMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly ContactService _contactService;

        public ContactServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<Contact>>();
            _replyValidatorMock = new Mock<IValidator<ContactReply>>();
            _emailServiceMock = new Mock<IEmailService>();
            // Replies are well-formed unless a test replaces this; keeps the
            // send/persist tests focused on ordering, not on validation.
            _replyValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ContactReply>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            // Staging a reply returns the instance it was handed (an unsupplied
            // Moq Task<ContactReply> resolves to null and the await throws).
            _unitOfWorkMock
                .Setup(u => u.ContactReplies.AddAsync(It.IsAny<ContactReply>()))
                .ReturnsAsync((ContactReply reply) => reply);
            _contactService = new ContactService(
                _unitOfWorkMock.Object,
                _validatorMock.Object,
                _replyValidatorMock.Object,
                _emailServiceMock.Object,
                new Mock<ILogger<ContactService>>().Object);
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

        [Fact]
        public async Task CreateContactAsync_ShouldSendAdminNotificationEmail()
        {
            // Arrange
            var contact = new Contact { Name = "Jane", Email = "jane@example.com", Subject = "Hi", Message = "Hello" };
            var createdContact = new Contact { Id = 7, Name = "Jane", Email = "jane@example.com", Subject = "Hi", Message = "Hello" };
            _validatorMock.Setup(v => v.ValidateAsync(contact, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _unitOfWorkMock.Setup(u => u.Contacts.AddAsync(It.IsAny<Contact>())).ReturnsAsync(createdContact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _contactService.CreateContactAsync(contact);

            // Assert — the notification was attempted with the STORED values
            result.Should().Be(createdContact);
            _emailServiceMock.Verify(
                e => e.SendContactFormEmailAsync("Jane", "jane@example.com", "Hi", "Hello"), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ShouldStillSucceed_WhenEmailSendingFails()
        {
            // Arrange — SMTP outage / unconfigured credentials must never fail
            // the visitor's submission.
            var contact = new Contact { Name = "Jane", Email = "jane@example.com", Message = "Hello" };
            var createdContact = new Contact { Id = 8, Name = "Jane", Email = "jane@example.com", Message = "Hello" };
            _validatorMock.Setup(v => v.ValidateAsync(contact, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _unitOfWorkMock.Setup(u => u.Contacts.AddAsync(It.IsAny<Contact>())).ReturnsAsync(createdContact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _emailServiceMock.Setup(e => e.SendContactFormEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP not configured"));

            // Act — must NOT throw despite the email failure
            var result = await _contactService.CreateContactAsync(contact);

            // Assert — the stored contact comes back regardless
            result.Should().Be(createdContact);
            _emailServiceMock.Verify(
                e => e.SendContactFormEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateContactAsync_ShouldNotSendEmail_WhenValidationFails()
        {
            // Arrange
            var contact = new Contact { Name = "", Email = "bad", Message = "" };
            var validationResult = new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new FluentValidation.Results.ValidationFailure("Name", "Name is required.")
                });
            _validatorMock.Setup(v => v.ValidateAsync(contact, default)).ReturnsAsync(validationResult);

            // Act
            Func<Task> act = async () => await _contactService.CreateContactAsync(contact);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _emailServiceMock.Verify(
                e => e.SendContactFormEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        private static Contact RepliedContact(string subject = "Project quote") => new()
        {
            Id = 1,
            Name = "Jane",
            Email = "jane@example.com",
            Subject = subject,
            Message = "Can you build a dashboard?",
            IsRead = false
        };

        [Fact]
        public async Task ReplyToContactAsync_ShouldDeliverBeforeStoring_AndMarkTheMessageRead()
        {
            var contact = RepliedContact();
            var calls = new List<string>();
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _emailServiceMock
                .Setup(e => e.SendContactReplyAsync("jane@example.com", "Re: Project quote", "Happy to help."))
                .Callback(() => calls.Add("sent"))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.ContactReplies.AddAsync(It.IsAny<ContactReply>()))
                .Callback(() => calls.Add("stored"))
                .ReturnsAsync(new ContactReply());

            var reply = await _contactService.ReplyToContactAsync(1, "  Happy to help.  ");

            reply.ContactId.Should().Be(1);
            reply.RecipientEmail.Should().Be("jane@example.com");
            reply.Subject.Should().Be("Re: Project quote");
            reply.Body.Should().Be("Happy to help.");
            reply.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
            contact.IsRead.Should().BeTrue();
            contact.ReadAt.Should().NotBeNull();

            // The whole point of send-first: the inbox history can never show a
            // reply that was not actually delivered.
            calls.Should().ContainInOrder("sent", "stored");
            _unitOfWorkMock.Verify(u => u.Contacts.UpdateAsync(contact), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReplyToContactAsync_ShouldNotDoublePrefix_AnAlreadyThreadedSubject()
        {
            var contact = RepliedContact("Re: Update on the quote");
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var reply = await _contactService.ReplyToContactAsync(1, "Sounds good.");

            reply.Subject.Should().Be("Re: Update on the quote");
        }

        [Fact]
        public async Task ReplyToContactAsync_ShouldThrowEmailDeliveryException_AndStoreNothing_WhenSendingFails()
        {
            var contact = RepliedContact();
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _emailServiceMock
                .Setup(e => e.SendContactReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP host rejected the connection"));

            Func<Task> act = async () => await _contactService.ReplyToContactAsync(1, "Happy to help.");

            await act.Should().ThrowAsync<EmailDeliveryException>();
            contact.IsRead.Should().BeFalse();
            _unitOfWorkMock.Verify(u => u.ContactReplies.AddAsync(It.IsAny<ContactReply>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ReplyToContactAsync_ShouldThrowValidationException_WhenBodyIsEmpty()
        {
            var contact = RepliedContact();
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(contact);
            _replyValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ContactReply>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                    new List<FluentValidation.Results.ValidationFailure>
                    {
                        new FluentValidation.Results.ValidationFailure("Body", "Write a reply before sending it.")
                    }));

            Func<Task> act = async () => await _contactService.ReplyToContactAsync(1, "   ");

            await act.Should().ThrowAsync<ValidationException>();
            _emailServiceMock.Verify(
                e => e.SendContactReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.ContactReplies.AddAsync(It.IsAny<ContactReply>()), Times.Never);
        }

        [Fact]
        public async Task ReplyToContactAsync_ShouldThrowNotFoundException_WhenContactIsMissing()
        {
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(99)).ReturnsAsync((Contact?)null);

            Func<Task> act = async () => await _contactService.ReplyToContactAsync(99, "Hello");

            await act.Should().ThrowAsync<NotFoundException>();
            _emailServiceMock.Verify(
                e => e.SendContactReplyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetRepliesAsync_ShouldReturnOnlyThatThreadsReplies_NewestFirst()
        {
            var older = new ContactReply { Id = 1, ContactId = 1, SentAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            var newer = new ContactReply { Id = 2, ContactId = 1, SentAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) };
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(1)).ReturnsAsync(RepliedContact());
            _unitOfWorkMock.Setup(u => u.ContactReplies.FindAsync(It.IsAny<Expression<Func<ContactReply, bool>>>()))
                .ReturnsAsync(new List<ContactReply> { older, newer });

            var result = await _contactService.GetRepliesAsync(1);

            result.Should().Equal(newer, older);
        }

        [Fact]
        public async Task GetRepliesAsync_ShouldThrowNotFoundException_WhenContactIsMissing()
        {
            _unitOfWorkMock.Setup(u => u.Contacts.GetByIdAsync(99)).ReturnsAsync((Contact?)null);

            Func<Task> act = async () => await _contactService.GetRepliesAsync(99);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}