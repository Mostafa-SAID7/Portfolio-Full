using FluentValidation;
using Microsoft.Extensions.Logging;
using PortfolioAPI.Application.Exceptions;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;
using ValidationException = PortfolioAPI.Application.Exceptions.ValidationException;

namespace PortfolioAPI.Application.Services
{
    public class ContactService : CrudService<Contact>, IContactService
    {
        private readonly IEmailService _emailService;
        private readonly IValidator<ContactReply> _replyValidator;
        private readonly ILogger<ContactService> _logger;

        public ContactService(
            IUnitOfWork unitOfWork,
            IValidator<Contact> validator,
            IValidator<ContactReply> replyValidator,
            IEmailService emailService,
            ILogger<ContactService> logger)
            : base(unitOfWork, validator)
        {
            _emailService = emailService;
            _replyValidator = replyValidator;
            _logger = logger;
        }

        protected override IRepository<Contact> Repository => UnitOfWork.Contacts;

        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await UnitOfWork.Contacts.GetAllAsync();
        }

        public async Task<Contact> GetContactByIdAsync(int id)
        {
            return await GetRequiredAsync(id);
        }

        public async Task<IEnumerable<Contact>> GetUnreadContactsAsync()
        {
            return await UnitOfWork.Contacts.FindAsync(c => !c.IsRead);
        }

        public async Task<Contact> CreateContactAsync(Contact contact)
        {
            var created = await CreateValidatedAsync(contact);

            // Best-effort admin notification (matches the integration tests'
            // expectation). Email problems — unconfigured SMTP, provider
            // outage — must NEVER fail the visitor's submission, so they are
            // swallowed into a warning log.
            try
            {
                await _emailService.SendContactFormEmailAsync(created.Name, created.Email, created.Subject, created.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Contact #{ContactId} was stored but the admin notification email could not be sent.", created.Id);
            }

            return created;
        }

        public async Task MarkAsReadAsync(int id)
        {
            // Deliberately NOT routed through the validating update helper:
            // this only flips IsRead/ReadAt, and re-validating stored content
            // here could 400 on legacy rows that predate current rules.
            var contact = await GetRequiredAsync(id);
            contact.MarkRead();
            await Repository.UpdateAsync(contact);
            await UnitOfWork.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(int id)
        {
            await DeleteOrThrowAsync(id);
        }

        public async Task<ContactReply> ReplyToContactAsync(int id, string body)
        {
            var contact = await GetRequiredAsync(id);
            var reply = ContactReply.ForVisitor(contact, body);

            var validationResult = await _replyValidator.ValidateAsync(reply);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // Send BEFORE storing: a reply only appears in the inbox history once
            // it actually left the server. Mail failures become a typed 503 so
            // the dashboard says "not sent" instead of blaming the text.
            try
            {
                await _emailService.SendContactReplyAsync(reply.RecipientEmail, reply.Subject, reply.Body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reply to contact #{ContactId} could not be delivered; nothing was stored.", id);
                throw new EmailDeliveryException("The reply could not be sent — the mail server rejected or did not answer the attempt. Your text was not lost; try again or use the mail link.", ex);
            }

            // Stamped at delivery, not at composition.
            reply.SentAt = DateTime.UtcNow;
            await UnitOfWork.ContactReplies.AddAsync(reply);

            // The visitor has been answered — clear the unread flag in the same
            // commit so the inbox count cannot stay stuck on a replied message.
            contact.MarkRead();
            await UnitOfWork.Contacts.UpdateAsync(contact);
            await UnitOfWork.SaveChangesAsync();

            return reply;
        }

        public async Task<IEnumerable<ContactReply>> GetRepliesAsync(int id)
        {
            await GetRequiredAsync(id);
            var replies = await UnitOfWork.ContactReplies.FindAsync(r => r.ContactId == id);
            return replies.OrderByDescending(r => r.SentAt);
        }
    }
}
