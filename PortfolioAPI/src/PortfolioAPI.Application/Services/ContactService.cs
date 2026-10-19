using FluentValidation;
using Microsoft.Extensions.Logging;
using PortfolioAPI.Core.Entities;
using PortfolioAPI.Core.Interfaces;
using PortfolioAPI.Core.Interfaces.Services;

namespace PortfolioAPI.Application.Services
{
    public class ContactService : CrudService<Contact>, IContactService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactService> _logger;

        public ContactService(
            IUnitOfWork unitOfWork,
            IValidator<Contact> validator,
            IEmailService emailService,
            ILogger<ContactService> logger)
            : base(unitOfWork, validator)
        {
            _emailService = emailService;
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
    }
}
