using System.Net;
using Bogus;
using FluentAssertions;
using PortfolioAPI.Core.DTOs;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.IntegrationTests
{
    public class ContactApiTests : TestBase
    {
        private readonly Faker<Contact> _contactFaker;

        public ContactApiTests()
        {
            _contactFaker = new Faker<Contact>()
                .RuleFor(c => c.Name, f => f.Name.FullName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.Subject, f => f.Lorem.Sentence(5))
                .RuleFor(c => c.Message, f => f.Lorem.Paragraphs(2))
                .RuleFor(c => c.IsRead, f => false);
        }

        [Fact]
        public async Task GetAllContacts_ReturnsEmptyList_WhenNoContactsExist()
        {
            // Act
            var response = await GetAsync("api/contact");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var contacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(response);
            contacts.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllContacts_ReturnsAllContacts_WhenContactsExist()
        {
            // Arrange
            var testContacts = _contactFaker.Generate(3);
            await _dbContext.Contacts.AddRangeAsync(testContacts);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/contact");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var contacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(response);
            contacts.Should().HaveCount(3);
            contacts.Should().AllSatisfy(c =>
            {
                c.Id.Should().BeGreaterThan(0);
                c.Name.Should().NotBeNullOrEmpty();
                c.Email.Should().NotBeNullOrEmpty();
                c.Message.Should().NotBeNullOrEmpty();
                c.IsRead.Should().BeFalse();
                c.CreatedAt.Should().NotBe(default);
                c.UpdatedAt.Should().NotBe(default);
            });
        }

        [Fact]
        public async Task GetContactById_ReturnsContact_WhenContactExists()
        {
            // Arrange
            var testContact = _contactFaker.Generate();
            await _dbContext.Contacts.AddAsync(testContact);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync($"api/contact/{testContact.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var contact = await DeserializeResponseAsync<ContactDto>(response);
            contact.Should().NotBeNull();
            contact!.Id.Should().Be(testContact.Id);
            contact.Name.Should().Be(testContact.Name);
            contact.Email.Should().Be(testContact.Email);
            contact.Message.Should().Be(testContact.Message);
        }

        [Fact]
        public async Task GetContactById_ReturnsNotFound_WhenContactDoesNotExist()
        {
            // Act
            var response = await GetAsync("api/contact/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUnreadContacts_ReturnsOnlyUnreadContacts()
        {
            // Arrange
            var unreadContact = _contactFaker.Generate();
            unreadContact.IsRead = false;

            var readContact = _contactFaker.Generate();
            readContact.IsRead = true;

            await _dbContext.Contacts.AddRangeAsync(unreadContact, readContact);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await GetAsync("api/contact/unread");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var contacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(response);
            contacts.Should().HaveCount(1);
            contacts.First().Id.Should().Be(unreadContact.Id);
            contacts.First().IsRead.Should().BeFalse();
        }

        [Fact]
        public async Task CreateContact_ReturnsCreatedContact_WhenValidDataProvided()
        {
            // Arrange
            var newContact = _contactFaker.Generate();

            // Act
            var response = await PostAsync("api/contact", newContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdContact = await DeserializeResponseAsync<ContactDto>(response);
            createdContact.Should().NotBeNull();
            createdContact!.Name.Should().Be(newContact.Name);
            createdContact.Email.Should().Be(newContact.Email);
            createdContact.Subject.Should().Be(newContact.Subject);
            createdContact.Message.Should().Be(newContact.Message);
            createdContact.IsRead.Should().BeFalse();
            createdContact.Id.Should().BeGreaterThan(0);
            createdContact.CreatedAt.Should().NotBe(default);
            createdContact.UpdatedAt.Should().NotBe(default);

            // Verify in database
            var dbContact = await _dbContext.Contacts.FindAsync(createdContact.Id);
            dbContact.Should().NotBeNull();
            dbContact!.Name.Should().Be(newContact.Name);
            dbContact.Email.Should().Be(newContact.Email);
        }

        [Fact]
        public async Task CreateContact_SendsEmailNotification_WhenContactIsCreated()
        {
            // Arrange
            var newContact = _contactFaker.Generate();
            var emailService = await GetServiceAsync<TestEmailService>();

            // Act
            var response = await PostAsync("api/contact", newContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verify email was sent
            emailService.SentEmails.Should().HaveCount(1);
            var sentEmail = emailService.SentEmails.First();
            sentEmail.Subject.Should().Contain("Contact");
            sentEmail.Body.Should().Contain(newContact.Name);
            sentEmail.Body.Should().Contain(newContact.Email);
            sentEmail.Body.Should().Contain(newContact.Message);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Name = string.Empty;

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenNameExceedsMaxLength()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Name = new string('A', 101); // Max length is 100

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenEmailIsEmpty()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Email = string.Empty;

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenEmailIsInvalid()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Email = "invalid-email";

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenSubjectExceedsMaxLength()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Subject = new string('A', 201); // Max length is 200

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenMessageIsEmpty()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Message = string.Empty;

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateContact_ReturnsBadRequest_WhenMessageExceedsMaxLength()
        {
            // Arrange
            var invalidContact = _contactFaker.Generate();
            invalidContact.Message = new string('A', 1001); // Max length is 1000

            // Act
            var response = await PostAsync("api/contact", invalidContact);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNoContent_WhenContactExists()
        {
            // Arrange
            var existingContact = _contactFaker.Generate();
            existingContact.IsRead = false;
            await _dbContext.Contacts.AddAsync(existingContact);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await PutAsync($"api/contact/{existingContact.Id}/read", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbContact = await _dbContext.Contacts.FindAsync(existingContact.Id);
            dbContact.Should().NotBeNull();
            dbContact!.IsRead.Should().BeTrue();
            dbContact.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNotFound_WhenContactDoesNotExist()
        {
            // Act
            var response = await PutAsync("api/contact/999/read", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteContact_ReturnsNoContent_WhenContactExists()
        {
            // Arrange
            var existingContact = _contactFaker.Generate();
            await _dbContext.Contacts.AddAsync(existingContact);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await DeleteAsync($"api/contact/{existingContact.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify in database
            var dbContact = await _dbContext.Contacts.FindAsync(existingContact.Id);
            dbContact.Should().BeNull();
        }

        [Fact]
        public async Task DeleteContact_ReturnsNotFound_WhenContactDoesNotExist()
        {
            // Act
            var response = await DeleteAsync("api/contact/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task EndToEndContactWorkflow_IncludingEmailNotification()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();

            // Create a contact
            var newContact = _contactFaker.Generate();
            var createResponse = await PostAsync("api/contact", newContact);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdContact = await DeserializeResponseAsync<ContactDto>(createResponse);

            // Verify email was sent
            emailService.SentEmails.Should().HaveCount(1);

            // Get the contact
            var getResponse = await GetAsync($"api/contact/{createdContact!.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var retrievedContact = await DeserializeResponseAsync<ContactDto>(getResponse);
            retrievedContact.Should().BeEquivalentTo(createdContact);

            // Mark as read
            var markReadResponse = await PutAsync($"api/contact/{createdContact.Id}/read", new { });
            markReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify marked as read
            var verifyReadResponse = await GetAsync($"api/contact/{createdContact.Id}");
            verifyReadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var verifiedContact = await DeserializeResponseAsync<ContactDto>(verifyReadResponse);
            verifiedContact!.IsRead.Should().BeTrue();
            verifiedContact.ReadAt.Should().NotBeNull();

            // Delete the contact
            var deleteResponse = await DeleteAsync($"api/contact/{createdContact.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify deletion
            var finalResponse = await GetAsync($"api/contact/{createdContact.Id}");
            finalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ContactFormSubmission_HandlesHighVolumeCorrectly()
        {
            // Arrange
            var emailService = await GetServiceAsync<TestEmailService>();
            var contacts = _contactFaker.Generate(10);

            // Act - Submit multiple contacts
            foreach (var contact in contacts)
            {
                var response = await PostAsync("api/contact", contact);
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }

            // Assert - Verify all contacts were created and emails sent
            var getAllResponse = await GetAsync("api/contact");
            getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var allContacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(getAllResponse);
            allContacts.Should().HaveCount(10);

            // Verify emails were sent for each contact
            emailService.SentEmails.Should().HaveCount(10);
            emailService.SentEmails.Should().AllSatisfy(email =>
            {
                email.Subject.Should().Contain("Contact");
                email.Body.Should().Contain("New Contact Form Submission");
            });
        }

        [Fact]
        public async Task ContactReadStatus_UpdatesCorrectly()
        {
            // Arrange - Create multiple contacts
            var contacts = _contactFaker.Generate(5);
            foreach (var contact in contacts)
            {
                await _dbContext.Contacts.AddAsync(contact);
            }
            await _dbContext.SaveChangesAsync();

            // Act - Mark first 3 as read
            for (int i = 0; i < 3; i++)
            {
                var response = await PutAsync($"api/contact/{contacts[i].Id}/read", new { });
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            // Assert - Verify unread count
            var unreadResponse = await GetAsync("api/contact/unread");
            unreadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var unreadContacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(unreadResponse);
            unreadContacts.Should().HaveCount(2);

            // Assert - Verify all contacts status
            var allResponse = await GetAsync("api/contact");
            allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var allContacts = await DeserializeResponseAsync<IEnumerable<ContactDto>>(allResponse);
            allContacts.Should().HaveCount(5);

            var readContacts = allContacts!.Where(c => c.IsRead).ToList();
            var unreadContactsList = allContacts.Where(c => !c.IsRead).ToList();

            readContacts.Should().HaveCount(3);
            unreadContactsList.Should().HaveCount(2);

            readContacts.Should().AllSatisfy(c => c.ReadAt.Should().NotBeNull());
            unreadContactsList.Should().AllSatisfy(c => c.ReadAt.Should().BeNull());
        }
    }
}